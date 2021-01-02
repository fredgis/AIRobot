# Modèle de Machine Learning / Deep Learning pour la detection d'anomalie 

Cette article présente l'algortihme de machine learning / deep learning utilisé pour détecter les anomalies des robots de la flotte.

### Introduction : 
Pour notre étude, nous voulons mettre en oeuvre un algorithme de machine learning / deep learning en python permettant de détecter les anomalies sur des robots dans le cadre d'une maintenance prédictive. Le modèle de machine learning / deep learning est par la suite appelé via Azure SQL Edge et requiert, par conséquent, le passage du modèle au format ONNX. Nous aborderons dans la suite le modèle que nous avons choisi pour détecter les anomalies des robots. Nous décrirons l'ensemble des étapes de développement de l'algorithme puis nous finirons par expliquer la démarche générale de mise en production du modèle sous ONNX.

### Choix du modèle :
Pour faire de la détection d'anomalie, nous avons à notre disposition plusieurs algorithmes. En plus des approches classiques de classification ou de clustering, des algortihmes spécifiques à la détection d'anomalie comme le One-Class SVM ou l'Isolation Forest peuvent s'avérer efficace. Des méthodes statistiques telles que ARIMA permettent également de faire de la détection d'anomalie (sur des séries temporelles). Dans notre étude, à la vue des données dont nous disposons et des résultats que nous souhaitons obtenir, nous réalisons un autoencodeur lstm. Cette approche est différente des algorithmes de détection d'anomalie classique. Mais elle offre plusieurs avantages. D'abord, c'est un algorithme de séries temporelles permettant de prendre en compte l'évolution des capteurs au cours du temps pour détecter une anomalie. Ensuite, la base de données doit seulement comporter des données sur le fonctionnement nominal du robot. Le modèle dira si le robot est en fonctionnement nominal ou s'il y a une anomalie. Il sera alors possible de détecter n'importe quel type d'anomalie (contrairement aux algorithmes ci-dessus qui détecteront les anomalies présentes dans la base de données d'entrainement). Par conséquent, la base n'a pas besoin d'être annotée ni de comporter un historique des pannes. C'est une approche plus générale d'un problème de maintenance prédictive.  

### Préparation des données :
Notre dataset comporte quatre features décrivant la vitesse de perçage, la température de perçage, le frottement du forêt et la température de l'eau de refroidissement de nos robots au cours du temps. A partir de ces données, nous allons construire un autoencodeur lstm. Nous n'avons pas de données manquantes. Une sélection de variables ou du feature enginieering ne sont pas des étapes utiles dans notre cas. Il faut par contre procéder à une normalisation des données. La méthode StandardScaler de Scikit Learn est utilisée ici. La deuxième étape de preprocessing est la mise en forme des données sous la forme de séries temporelles pour l'entrainement. Pour cela on utilise la fonction suivante permettant de créer des blocs de (60x4) correspondant à une série temporelle (60 périodes de temps avec les quatre features du dataset. X_final est donc un array de N blocs de (60x4).

```python
def mise_en_serie_temporelle(X):
    periode = 60
    X_final = []
    for i in range(len(X)-periode-1):
        t = []
        for j in range(1,periode+1):
            t.append(X[[(i+j+1)], :])
        X_final.append(t)
    X_final = np.array(X_final)
    X_final = X_final.reshape(X_final.shape[0],periode,4)
    return X_final
```

### Modèle :
Les données sont maintenant prêtes pour l'autoencodeur lstm. On dispose de données d'entrainement et de validation. Voici le modèle :

```python
#Définition du modèle
model = Sequential()
model.add(LSTM(180,  activation = 'relu', input_shape=(timesteps,n_features), return_sequences = True))
model.add(Dropout(d))
model.add(LSTM(120,   activation = 'relu', return_sequences = False))
model.add(Dropout(d))
model.add(RepeatVector(n = timesteps))
model.add(LSTM(120,   activation = 'relu', return_sequences=True))
model.add(Dropout(d))
model.add(LSTM(180,  activation = 'relu', return_sequences=True))
model.add(TimeDistributed(Dense(n_features)))
```
Un autoencodeur doit reconstruire l'entrée qui lui est fournie en passant d'abord par un encodeur puis par un décodeur. L'entrée est de dimension 3 (Nb de séries temporelles, Nb de période de temps (60), nombre de features (4)). C'est un modèle de type séquentiel avec deux couches cachées pour l'encodeur et le décodeur. On ajoute en premier lieu une couche de type LSTM avec 180 neurones puis une seconde couche LSTM de 120 neurones. Ces deux couches constituent la partie encodage de l'autoencodeur. La fonction RepeatVector réalise la transition entre la partie encodage et décodage. Elle transforme les données de dimension 2 qu'elle reçoit en entrée en 3 dimensions. Ensuite, on a la partie décodage avec deux layers LSTM de 120 et 180 neurones par symétrie avec l'encodage (par définition de l'autoencodeur). Enfin, on a une dernière couche qui permet de transformer l'entrée reçue de dimension 3 (Nb de séries, 60, Nombre de neurones de la couche (180)) en une entrée de dimension 3 (Nb de séries, 60, 4) correspondant à l'entrée de l'algorithme. On retrouve donc bien l'entrée de l'algorithme après avoir fait passer les données dans différentes couches, ce qui définit le principe de l'autoencodeur. Le dropout sert à prevenir l'overfitting.
Il ne reste plus qu'à compiler le modèle puis à l'entrainer. On utilise l'optimisateur Adam car il offre les meilleurs résultats ainsi que la fonction de coût mean_absolute_error, on y reviendra dans une prochaine section.

```python
#Compilation du modèle
adam = optimizers.Adam()
model.compile(loss='mae', optimizer=adam)

#Entrainement du modele
history = model.fit(X_train_std, X_train_std, 
                     validation_data=(X_valid_std,X_valid_std),
                     epochs=epochs, 
                     batch_size=batch)
```

### Evaluation / Seuil d'erreur :
Le modèle est entrainé à reconstruire les données générées par le robot en fonctionnement normal. L'objectif du modèle ci-dessus et d'attribuer une erreur de reconstruction à chaque série temporelle passée en entrée. C'est pour cela que la fonction de coût choisie est la mean_absolute_error mesurant l'erreur moyenne de la différence en valeur absolue entre les données d'entrées et de sorties. Une erreur faible signifie que le modèle est bien arrivé à reconstruire l'entrée donnée à l'algorithme, le robot est donc en fonctionnement normal. Une erreur élevée signifie que le modèle a rencontré des données éloignées des données normales sur lesquels il a été entrainé. Ces nouvelles données signifient donc que le robot est en fonctionnement anormal. Une erreur est faible si elle est inférieure à un seuil d'erreur et elevée si elle est supérieure à ce même seuil. Plus l'erreur est éloignée de ce dernier et plus le fonctionnement du robot est anormal. Déterminons graphiquement le seuil associé à notre modèle. Le graphique qui suit représente l'erreur de chaque série lors de la phase d'entrainement, c'est à dire en fonctionnement normal du robot. L'erreur de reconstruction maximale de la phase d'entrainement est de 0,9. C'est donc le seuil qui va nous servir à déterminer si les nouvelles données exploitées représentent le fonctionnement normal ou non du robot.
![](/Pictures/iRobotArchitecture.png?raw=true)

### Préparation de la mise en production sous ONNX :
Nous disposons de deux étapes différentes dans notre algorithme. Le prétraitement des données puis le modèle. Nous mettons dans un pipeline la standardisation des données puis nous convertissons cette pipeline en modèle ONNX à l'aide de la commande convert_sklearn du module skl2onnx. Pour le modèle, nous utilisons la commande convert_keras du module keras2onnx pour convertir le modèle en ONNX. Le script ci-dessus montre comment utiliser les deux fichiers ONNX. 

### Exemple de prédictions sur des nouvelles données:
Prenons maintenant de nouvelles données et voyons si le robot est en fonctionnement anormal.
Nous utilisons le script ci-dessous pour tester les modèles et les nouvelles données

```python
#data est un tableau de (60x4)
sess1 = rt.InferenceSession("pipeline_std.onnx")
pred_onnx_std = sess1.run(None, {sess1.get_inputs()[0].name: data})[0]

batch = np.array(pred_onnx_std,ndmin=3)

sess2 = rt.InferenceSession("model_final.onnx")
pred_onnx = sess2.run(None, {sess2.get_inputs()[0].name: batch})[0]

def erreur(a,b):
    err = 0
    for i in range(a.shape[0]):
        for j in range(a.shape[1]):
            err = err + abs(a[i][j] - b[i][j])
    err = err / (a.shape[0] * a.shape[1])        
    return err

print(erreur(pred_onnx_std,pred_onnx[0]))
```
Une importation des deux modèles est effectuée. Nous faisons passer les nouvelles données dans les modèles puis nous finissons par calculer l'erreur grâce à la fonction erreur. Si le résultat est inférieur au seuil, alors le robot est en fonctionnement normal sinon il est en fonctionnement anormal. Sur des données de plusieurs jours, on calcule l'erreur toute les minutes. Les résultats sont résumés sur le graphique ci-dessous. On s'aperçoit qu'il y a la détection d'une anomalie entre 4000 et 9000. Le fonctionnement du robot est normal le reste du temps. 
![](/Pictures/iRobotArchitecture.png?raw=true)

### Conclusion :
Le développement de ce modèle s'est déroulé en plusieurs étapes. Après le traitement des données, l'autoencodeur lstm est créé, entrainé puis une recherche des meilleurs hyperparamètres est effectuée. L'algorithme est ensuite exporté au format ONNX. Il est maintenant prêt à ingérer de nouvelles données afin de déterminer si le robot est en fonctionnement normal ou non.
