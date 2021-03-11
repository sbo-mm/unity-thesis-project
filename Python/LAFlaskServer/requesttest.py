import requests
import numpy as np
import json

vpath = "./vertices.txt"
tpath = "./triangles.txt"

verts = np.genfromtxt(vpath, delimiter=',')
trias = np.genfromtxt(tpath, delimiter=',')

cont = {
	"mesh":{
		"vertices": verts.tolist(),
		"triangles": trias.tolist()
	},

	"material":{
		"youngs": 10000,
		"thickness": 1000,
		"density": 1,
		"visco": 1e-10,
		"fluid": 1e-10
	}
}

url = 'http://127.0.0.1:5000/sphere:wood:unity'
header = {'Content-Type': 'application/json'}

r = requests.get(url, data=json.dumps(cont), headers=header)
model = json.loads(r.text)
print(model)
