import cmath
import scipy.linalg
import numpy as np

two_pi = 2 * np.pi

def csqrt(nparr):
    return np.array([cmath.sqrt(n) for n in nparr])

def swap_numpy_arr(arr, a, b):
	arr[[a, b]] = arr[[b, a]]

def arrange_heron(sidelens):
	a, b, c = sidelens
	if (a <= b):
		swap_numpy_arr(sidelens, 0, 1)
	if (a <= c):
		swap_numpy_arr(sidelens, 0, 2)
	if (b <= c):
		swap_numpy_arr(sidelens, 1, 2)

def heron_area(sidelens):
	arrange_heron(sidelens)
	a, b, c = sidelens
	h = (a + (b + c)) * (c - (a - b)) * (c + (a - b)) * (a + (b - c))
	return np.sqrt(h) / 12

class SpringMassSystem(object):

	def __init__(self):
		self.dof = 3

	def connect_elements(self, triangles):
		subtris = np.reshape(np.array(triangles), (-1, 3))
		self.connectivity_map = np.array([
			[l[0], l[1], l[1], l[2], l[2], l[0]] \
				for l in subtris
		]).astype(np.uint)

	def connect_nodes2tri(self, triangles, vertices):
		nnodes = len(vertices) // self.dof
		contained_in_tri = [[] for _ in range(nnodes)]
		subtris = np.reshape(
			np.array(triangles), (-1, 3)
		).astype(np.uint)

		for tridx in range(subtris.shape[0]):
			nodeidxs = subtris[tridx, :]
			contained_in_tri[nodeidxs[0]].append(tridx)
			contained_in_tri[nodeidxs[1]].append(tridx)
			contained_in_tri[nodeidxs[2]].append(tridx)

		return contained_in_tri

	def triangle_areas(self, triangles, vertices):
		lhnodes = self.connectivity_map[:, 0::2]
		rhnodes = self.connectivity_map[:, 1::2]

		verticepoints = np.reshape(vertices, (-1, 3))
		triangle_sidelengths = np.linalg.norm(
			verticepoints[lhnodes] - verticepoints[rhnodes],
			axis = -1
		)

		triangle_areas = np.apply_along_axis(
			heron_area, 1, triangle_sidelengths
		)

		return triangle_areas


	def form_stiffness(self, gdof, youngs, thickness):
		k = youngs * thickness
		localstiffness = np.array([
			[ k,  k,  k, -k, -k, -k],
			[ k,  k,  k, -k, -k, -k],
			[ k,  k,  k, -k, -k, -k],
			[-k, -k, -k,  k,  k,  k],
			[-k, -k, -k,  k,  k,  k],
			[-k, -k, -k,  k,  k,  k]
		])

		element_indices = np.reshape(self.connectivity_map, (-1, 2))

		K = np.zeros((gdof, gdof))

		for indice in element_indices:
			element_dof = np.array([
				3 * (indice[0] + 1) - 3,  3 * (indice[0] + 1) - 2, 3 * (indice[0] + 1) - 1,
				3 * (indice[1] + 1) - 3,  3 * (indice[1] + 1) - 2, 3 * (indice[1] + 1) - 1
			]).astype(np.uint)

			K[np.ix_(element_dof, element_dof)] += localstiffness

		return K


	def form_mass(self, triangles, vertices, gdof, density, thickness):

		conns = self.connect_nodes2tri(triangles, vertices)
		areas = self.triangle_areas(triangles, vertices)

		M = np.zeros((gdof, gdof))

		for nodeidx, contained in enumerate(conns):
			facesum = np.sum(areas[contained])
			nodemass = density * thickness * facesum

			node_dof = [
				3 * (nodeidx + 1) - 3, 3 * (nodeidx + 1) - 2, 3 * (nodeidx + 1) - 1
			]

			M[node_dof, node_dof] = nodemass

		return M


	def __call__(self, triangles, vertices, material):
		gdof = self.dof * (len(vertices) // 3)
		
		youngs    = material["youngs"]
		density   = material["density"]
		thickness = material["thickness"]

		self.connect_elements(triangles)
		M = self.form_mass(triangles, vertices, gdof, density, thickness)
		K = self.form_stiffness(gdof, youngs, thickness)
		return M, K


class ModalModel(object):

	def __init__(self):
		self.freqs = None
		self.decays = None
		self.gains = None

		t0 = 15; t1 = 2000; t2 = 8000;
		y0 = 3; y1 = 45; y2 = 90;
		m0 = (y1 - y0) / (t1 - t0);
		m1 = (y2 - y1) / (t2 - t1);
		self.dlc = lambda x: (y0 + m0*(x - t0)) * (x >= 0) * (x <= t1)\
		    + (y1 + m1*(x - t1)) * (x > t1)

	def evd(self, M, K):
		return scipy.linalg.eigh(K, M)

	def eigenfreqs(self, M, K, dampingcoefs):
		gamma = dampingcoefs[0]
		eta   = dampingcoefs[1]
		E, V  = self.evd(M, K)

		delta  = gamma * E + eta
		omega2 = 4 * E
		omega  = ((-delta) + csqrt(delta**2 - omega2)) / 2 

		self.gains = V
		return omega

	def precull(self, eigenfreqs):
		freqs = np.abs(eigenfreqs.imag) / two_pi

		fmin = 20
		fmax = 22000

		cullmask_min = freqs > fmin
		cullmask_max = freqs < fmax
		cullmask = cullmask_min & cullmask_max

		self.freqs  = freqs[cullmask]
		self.gains  = self.gains[:, cullmask]
		self.decays = eigenfreqs.real[cullmask]

	def aggregate(self):
		i = 0
		G = np.zeros_like(self.gains)

		while i < G.shape[1]:
			x = self.freqs[i]
			d = self.freqs[i:] - x
			delta = self.dlc(x)
			predmask = d < delta
			aggregated = self.gains[:, i:][:, predmask]
			G[:, i] = np.sum(aggregated, axis=1)
			numaggregated = len(aggregated[0, :]) 
			i += numaggregated

		predsum = np.sum(G, axis=0)
		predidx = np.argwhere(np.all(G[..., :] == 0, axis=0))
		self.gains = np.delete(G, predidx, axis=1)
		self.freqs = np.delete(self.freqs, predidx, axis=-1)
		self.decays = np.delete(self.decays, predidx, axis=-1)

	def modelobj(self):
		nverts, nmodes = self.gains.shape
		mmfreqs = self.freqs.flatten()
		mmfreqs = np.around(mmfreqs, 3)
		mmdecays = self.decays.flatten()
		mmgains = self.gains.flatten()
		mmgains = np.around(mmgains, 7)

		model = {
			"vertices": nverts,
			"modes": nmodes,
			"freqs": mmfreqs.tolist(),
			"decays": mmdecays.tolist(),
			"gains": mmgains.tolist()
		}

		return model

	def __call__(self, massmatrix, stiffnessmatrix, dampingcoefs):
		omega = self.eigenfreqs(massmatrix, stiffnessmatrix, dampingcoefs) 
		self.precull(omega)
		self.aggregate()
		return self.modelobj()

		
		