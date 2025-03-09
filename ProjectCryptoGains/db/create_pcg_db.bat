@ECHO off
ECHO Deleting existing pcg.fdb file
del /q pcg.fdb

ECHO.
ECHO Creating PCG.FDB
isql.exe -u SYSDBA -p masterkey -i pcg_fdb.ddl

ECHO.
ECHO Renaming PCG.FDB to lowercase pcg.fdb
rename PCG.FDB pcg.fdb
pause