import pandas as pd
import numpy as np
from random import uniform,randint,sample
import datetime
import math

temperature_percage = 70
frottement_foret = 0.5
vitesse_percage = 1500
temperature_eau_refroidissement = 10

long_dataset = 28800*20

def choose():
    L = [0,1,2,3]
    return sample(L,3)

def fonction_pos_tp(x):
    #plus 10 pour temperature_percage
    return x*0.001737 + uniform(-2,2)

def fonction_neg_tp(x):
    #moins 10 pour temperature_percage
    return x*(-1*0.001737) + uniform(-2,2)

def fonction_pos_ff(x):
    #plus 0.4 pour temperature_percage
    return x*0.00007 + uniform(-0.1,0.1)

def fonction_neg_ff(x):
    #moins 0,4 pour temperature_percage
    return x*(-0.00007) + uniform(-0.1,0.1)

def fonction_pos_vp(x):
    #plus 1500 pour temperature_percage
    return x*0.0864 + uniform(-50,50)

def fonction_neg_vp(x):
    #moins 1000 pour temperature_percage
    return x*(-1*0.0864) + uniform(-50,50)

def fonction_pos_ter(x):
    #plus 5 pour temperature_percage
    return x*(0.000868) + uniform(-1,1)

def fonction_neg_ter(x):
    #moins 5 pour temperature_percage
    return x*(-1*0.000868) + uniform(-1,1)

def generate_dataset(lg_ds):  
   
    Temps = [datetime.datetime(2010,1,1,0,0,0)]
    Temp_perc = [temperature_percage]
    Frot_foret = [frottement_foret]
    Vitesse_perc = [vitesse_percage]
    Temp_refr = [temperature_eau_refroidissement]
    Target = [0]
    
    for i in range(lg_ds):
        if ((i % 28800) == 0):
        
            a = i + randint(1, 23040)
        
        if (i == a):
            
            b = choose()
            c1 = randint(0, 1)
            c2 = randint(0, 1)
            c3 = randint(0, 1)
            c4 = randint(0, 1)
            
            for j in range(5760):
                
                Temps.append(Temps[-1] + datetime.timedelta(minutes=5))

                if ((0 in b) and (c1 == 0)):
                    Temp_perc.append(temperature_percage + fonction_pos_tp(j))
                elif ((0 in b) and (c1 == 1)):
                    Temp_perc.append(temperature_percage + fonction_neg_tp(j))
                else :
                    Temp_perc.append(temperature_percage + uniform(-2,2))
                
                if ((1 in b) and (c2 == 0)):
                    Frot_foret.append(frottement_foret + fonction_pos_ff(j))
                elif ((1 in b) and (c2 == 1)):
                    Frot_foret.append(frottement_foret + fonction_neg_ff(j))
                else :
                    Frot_foret.append(frottement_foret + uniform(-0.1,0.1))
                    
                if ((2 in b) and (c3 == 0)):
                    Vitesse_perc.append(vitesse_percage + fonction_pos_vp(j))
                elif ((2 in b) and (c3 == 1)):
                    Vitesse_perc.append(vitesse_percage + fonction_neg_vp(j))
                else :
                    Vitesse_perc.append(vitesse_percage + uniform(-50,50))
                    
                if ((3 in b) and (c4 == 0)):
                    Temp_refr.append(temperature_eau_refroidissement + fonction_pos_ter(j))
                elif ((3 in b) and (c4 == 1)):
                    Temp_refr.append(temperature_eau_refroidissement + fonction_neg_ter(j))
                else :
                    Temp_refr.append(temperature_eau_refroidissement + uniform(-1,1))
                                
                if (j > 1000):
                    Target.append(1)
                else :
                    Target.append(0)

        else :

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

df.to_csv(r'C:\Users\teywo\OneDrive\Bureau\iRobot\Dataset_iRobot_with_anomaly.csv', index = False,header=True)
