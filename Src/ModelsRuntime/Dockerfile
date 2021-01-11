FROM ubuntu:18.04

RUN apt-get update
RUN apt-get --assume-yes install wget

RUN wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb

RUN apt-get update; \
    apt-get --assume-yes install -y apt-transport-https && \
    apt-get update && \
    apt-get --assume-yes install -y dotnet-runtime-3.1

RUN apt-get --assume-yes install p7zip

RUN apt-get install libgomp1

WORKDIR /home
RUN mkdir afpredict
WORKDIR /home/afpredict
RUN wget https://raw.githubusercontent.com/fredgis/AIRobot/main/Builds/ModelsRuntime/Linux-x64/publish.7z?token=APXEJQZ2CQYC3CU4QOOXSWDAAWAPW
RUN mv publish.7z?token=APXEJQZ2CQYC3CU4QOOXSWDAAWAPW publish.7z
RUN 7zr e publish.7z
RUN rm publish.7z

RUN apt-get --assume-yes remove p7zip
RUN apt-get --assume-yes remove wget
RUN apt-get clean

CMD ["dotnet", "afpredict.dll"]