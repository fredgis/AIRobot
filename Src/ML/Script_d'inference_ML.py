import onnxruntime as rt
import pandas as pd
import numpy as np

df_ano = pd.read_csv('Dataset_iRobot_with_anomaly.csv', index_col='temps')
df_ano.index = pd.to_datetime(df_ano.index)
df_ano = df_ano.iloc[5000:5060,:].values

sess1 = rt.InferenceSession("pipeline_std.onnx")

pred_onnx_std = sess1.run(None, {sess1.get_inputs()[0].name: df_ano})[0]

sess2 = rt.InferenceSession("model_final.onnx")

batch = np.array(pred_onnx_std,ndmin=3)

pred_onnx = sess2.run(None, {sess2.get_inputs()[0].name: batch})[0]

def erreur(a,b):
    err = 0
    for i in range(a.shape[0]):
        for j in range(a.shape[1]):
            err = err + abs(a[i][j] - b[i][j])
    err = err / (a.shape[0] * a.shape[1])        
    return err

print(erreur(pred_onnx_std,pred_onnx[0]))