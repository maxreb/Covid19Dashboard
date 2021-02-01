@echo off
docker build --no-cache -t hse-server01:5000/reble/covid19dashboard -f .\Covid19Dashboard\Dockerfile .
docker push hse-server01:5000/reble/covid19dashboard
pause
