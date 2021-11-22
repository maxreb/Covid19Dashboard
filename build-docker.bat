@echo off
cd ..
where docker >nul 2>nul
IF ERRORLEVEL 0 (
    bash -c "docker build --no-cache -t hse-server01:5000/reble/covid19dashboard -f ./Covid19Dashboard/Covid19Dashboard/Dockerfile ."
	bash -c "docker push hse-server01:5000/reble/covid19dashboard"
) ELSE (
	docker build --no-cache -t hse-server01:5000/reble/covid19dashboard -f .\Covid19Dashboard\Covid19Dashboard\Dockerfile .
	docker push hse-server01:5000/reble/covid19dashboard
)
pause
