import numpy as np
from modalanalysis import SpringMassSystem, ModalModel
from flask import Flask, Response, request
from flask_restful import Resource, Api

app = Flask(__name__)
api = Api(app)

class mmAPI_ExistingModel(Resource):

	def get(self, model_id):
		return {}, 404 
		
class mmAPI_ExistingModelList(Resource):
	
	def get(self):
		return {}, 404
		

class mmAPI_ModalModel(Resource):

	@staticmethod
	def unpack_vertices(vlist):
		if not any(isinstance(x, dict) for x in vlist):
			return vlist

		vertli = [
			i for d in vlist for i in (d["x"], d["y"], d["z"])
		]

		return vertli

	@staticmethod
	def make_model(model_properties):
		mesh = model_properties["mesh"]
		material = model_properties["material"]

		triangles = mesh["triangles"]
		vertices  = mmAPI_ModalModel.unpack_vertices(mesh["vertices"])
		M, K = SpringMassSystem()(triangles, vertices, material)

		visco_damping = material["visco"]
		fluid_damping = material["fluid"]
		damping = [visco_damping, fluid_damping]
		model = ModalModel()(M, K, damping)
		return model

	def get(self, model_id):
		cont = request.json
		# TODO: validate json

		print(cont)

		model = mmAPI_ModalModel.make_model(cont)
		model["id"] = model_id

		return model
		
	def post(self, model_id):
		cont = request.json
		# TODO: validate json

		print(cont)

		model = mmAPI_ModalModel.make_model(cont)
		model["id"] = model_id

		print(model)
		# TODO: store model

		return model

	def put(self, model_id):
		return {}, 200


api.add_resource(mmAPI_ExistingModelList, '/mmapi/modalmodel')
api.add_resource(mmAPI_ExistingModel, '/mmapi/modalmodel/<string:model_id>')
api.add_resource(mmAPI_ModalModel, '/<string:model_id>')

if __name__ == '__main__':
    app.run(debug=True)
