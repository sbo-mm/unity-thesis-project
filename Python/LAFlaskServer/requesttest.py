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

#73034021-

url = 'http://127.0.0.1:5000/sphere:wood:unity'
header = {
	"X-CoinAPI-Key": 'E9FD629F-4AB6-49B7-90F5-EC08B85C2A12',
	"Accept": 'application/json',
	'Accept-Encoding': 'deflate, gzip'
}

#r = requests.get(url, data=json.dumps(cont), headers=header)
url = 'https://rest.coinapi.io/v1/exchanges/{BTC}'
r = requests.get(url, headers=header)
model = json.loads(r.text)
print(model)
