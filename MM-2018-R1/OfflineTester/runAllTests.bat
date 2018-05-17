FOR /L %%A IN (1,1,1000) DO (
  java -jar Tester.jar -exec "..\bin\Release\MM-2018-R1.exe -offline" -seed %%A >>result.txt
)
