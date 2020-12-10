import pandas as pd
import numpy as np
from random import uniform,randint,sample
import datetime
import math

temperature_percage = 70
frottement_foret = 0.5
vitesse_percage = 1500
temperature_eau_refroidissement = 10

long_dataset = 28800*10

def generate_dataset(lg_ds):  
   
    Temps = [datetime.datetime(2010,1,1,0,0,0)]
    Temp_perc = [temperature_percage]
    Frot_foret = [frottement_foret]
    Vitesse_perc = [vitesse_percage]
    Temp_refr = [temperature_eau_refroidissement]
    Target = [0]
    
    for i in range(lg_ds):
        Temps.append(Temps[-1] + datetime.timedelta(minutes=5))
        Temp_perc.append(temperature_percage + 2*(np.sin(i/(288))) + uniform(-0.4, 0.4))
        Frot_foret.append(frottement_foret + uniform(-0.1,0.1))
        Vitesse_perc.append(vitesse_percage + 50*(np.sin(i/(288))) + uniform(-10, 10))
        Temp_refr.append(temperature_eau_refroidissement + np.cos(i/(288)) + uniform(-0.2,0.2))
        Target.append(0)

    df = pd.DataFrame([Temps,Temp_perc,Frot_foret,Vitesse_perc,Temp_refr,Target]).transpose()
    columns = ["temps","temp_perc","frot_foret","vit_perc","temp_eau_refr","target"]
    df.columns = columns
    
    return df

#Chaque 28800 génère un nombre entre 1 et 23040
#Dérive pdt 5760 rows avant la panne 

df = generate_dataset(long_dataset)

df.to_csv(r'C:\Users\teywo\OneDrive\Bureau\iRobot\Dataset_iRobot_without_anomaly.csv', index = False,header=True)
