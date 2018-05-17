FOR /L %%A IN (1,1,1000) DO (
  java -jar Tester.jar -exec "..\bin\Debug\MM-2018-R1.exe ..\inputs\%%A.txt" -seed %%A
)
