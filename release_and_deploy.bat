cd %~dp0
del Release\*.pdb Release\*.xml
mkdir Deploy
xcopy /E /Y Release Deploy
xcopy /E /Y real_data Deploy
pause