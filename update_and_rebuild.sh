#!/bin/bash
sudo systemctl stop prdelbot.service
git pull
sudo dotnet restore
sudo dotnet publish --configuration Release --output bin
sudo chown -R www-data:www-data .
sudo systemctl daemon-reload
sudo systemctl start prdelbot.service