import pandas as pd
import numpy as np
import seaborn as sns
from keras.models import Sequential
from keras.layers import Dense,LSTM,Dropout,RepeatVector,TimeDistributed
from sklearn.preprocessing import StandardScaler
from sklearn.model_selection import train_test_split
import matplotlib.pyplot as plt

df = pd.read_csv('Dataset_iRobot_without_anomaly.csv', index_col='temps')
df.index = pd.to_datetime(df.index)

df_ano = pd.read_csv('Dataset_iRobot_with_anomaly.csv', index_col='temps')
df_ano.index = pd.to_datetime(df_ano.index)
df_ano = df_ano.iloc[:28800,:]


df2 = pd.read_csv('Dataset_iRobot_with_anomaly.csv')
df2['temps'] = pd.to_datetime(df2['temps'])
df2 = df2.iloc[:28000,:]

df2.plot(kind='scatter',x='temps',y='temp_perc',color='red')
plt.show()

df2.plot(kind='scatter',x='temps',y='frot_foret',color='red')
plt.show()

df2.plot(kind='scatter',x='temps',y='vit_perc',color='red')
plt.show()

df2.plot(kind='scatter',x='temps',y='temp_eau_refr',color='red')
plt.show()


fig,ax = plt.subplots(figsize=(14,6), dpi =80)
ax.plot(df_ano['temp_perc'], label = 'temp_perc', color = 'red', linewidth = 1)
ax.plot(df_ano['frot_foret'], label = 'frot_foret', color = 'blue', linewidth = 1)
ax.plot(df_ano['vit_perc'], label = 'vit_perc', color = 'green', linewidth = 1)
ax.plot(df_ano['temp_eau_refr'], label = 'temp_eau_refr', color = 'orange', linewidth = 1)
plt.legend()
plt.title("Visulation des données en fonction du temps")


fig,ax = plt.subplots(figsize=(14,6), dpi =80)
ax.plot(df['temp_perc'], label = 'temp_perc', color = 'red', linewidth = 1)
ax.plot(df['frot_foret'], label = 'frot_foret', color = 'blue', linewidth = 1)
ax.plot(df['vit_perc'], label = 'vit_perc', color = 'green', linewidth = 1)
ax.plot(df['temp_eau_refr'], label = 'temp_eau_refr', color = 'orange', linewidth = 1)
plt.legend()
plt.title("Visulation des données en fonction du temps")


#X_train, X_test = train_test_split(df, test_size=0.25, shuffle=False)

X_train = df 
X_test = df_ano

y_train = X_train['target'].values
y_test = X_test['target'].values

del X_train['target']
del X_test['target']

std = StandardScaler()
X_train_std = std.fit_transform(X_train)
X_test_std = std.transform(X_test)

X_train_std = X_train_std.reshape(X_train.shape[0],1,X_train.shape[1])
X_test_std = X_test_std.reshape(X_test.shape[0],1,X_test.shape[1])

'''
def autoencodeur_lstm(X_train):
    inputs = Input(shape = (X_train.shape[1],X_train.shape[2]))
    L1 = LSTM(units=16,activation = 'relu')(inputs)
    L2 = LSTM(units=4,activation = 'relu')(L1)   
    L3 = RepeatVector(n = X_train.shape[1])(L2)
    L4 = LSTM(units = 4, return_sequences=True)(L3)
    L5 = LSTM(units=16,activation = 'relu')(L4)
    output = TimeDistributed(Dense(units = X_train.shape[2]))(L5)
    model = Model(inputs = inputs, outputs = output)
    return model
'''

model = Sequential()
model.add(LSTM(units=20,activation = 'relu', input_shape=(X_train_std.shape[1],X_train_std.shape[2]), return_sequences = True))
#model.add(Dropout(rate = 0.01))
#model.add(LSTM(units = 15, return_sequences=True))
model.add(LSTM(units=4,activation = 'relu',return_sequences = False))
model.add(RepeatVector(n = X_train_std.shape[1]))
model.add(LSTM(units = 4, return_sequences=True))
#model.add(Dropout(rate = 0.01))
#model.add(LSTM(units = 15, return_sequences=True))
model.add(LSTM(units = 20, return_sequences=True))
model.add(TimeDistributed(Dense(units = X_train_std.shape[2])))

model.compile(loss='mae', optimizer='adam')

print(model.summary())

nb_epoch = 10
batch_size = 1000
history = model.fit(X_train_std,X_train_std, epochs = nb_epoch, batch_size = batch_size, validation_split = 0.2)

history = history.history

fig,ax = plt.subplots(figsize = (14,6), dpi = 80)
ax.plot(history['loss'],'b', label = 'Train', linewidth = 1)
ax.plot(history['val_loss'],'r', label = 'Validation', linewidth = 1)
ax.set_title('Model_loss', fontsize = 16)
ax.set_ylabel('Loss MAE')
ax.set_xlabel('Epoch')
ax.legend()
plt.show()

X_pred = model.predict(X_train_std)
X_pred = X_pred.reshape(X_pred.shape[0], X_pred.shape[2])
df_X_pred = pd.DataFrame(X_pred, columns=['temp_perc', 'frot_foret', 'vit_perc', 'temp_eau_refr'])
df_X_pred.index = X_train.index

a = X_train_std.reshape(X_train_std.shape[0], X_train_std.shape[2])
df_X_train = pd.DataFrame(a, columns=['temp_perc', 'frot_foret', 'vit_perc', 'temp_eau_refr'])
df_X_train.index = X_train.index

scored = pd.DataFrame(index=X_train.index)
scored['Loss_MAE'] = np.mean(np.abs(df_X_pred-df_X_train),axis = 1)

plt.figure(figsize=(16,9), dpi=80)
plt.title('Loss Distribution', fontsize=16)
sns.distplot(scored['Loss_MAE'], bins = 5, kde= True, color = 'blue');
plt.xlim([0.0,0,5])


X_pred2 = model.predict(X_test_std)
X_pred2 = X_pred2.reshape(X_pred2.shape[0], X_pred2.shape[2])
X_pred2 = pd.DataFrame(X_pred2, columns=['temp_perc', 'frot_foret', 'vit_perc', 'temp_eau_refr'])
X_pred2.index = X_test.index

scored2 = pd.DataFrame(index=X_test.index)
df_X_test = X_test_std.reshape(X_test_std.shape[0], X_test_std.shape[2])
scored2['Loss_mae'] = np.mean(np.abs(X_pred2-df_X_test), axis = 1)
scored2['Threshold'] = 0.175
scored2['Anomaly'] = scored2['Loss_mae'] > scored2['Threshold']
scored2.head()

# plot bearing failure time plot
scored2.plot(logy=True,  figsize=(16,9), ylim=[1e-2,1e2], color=['blue','red'])



