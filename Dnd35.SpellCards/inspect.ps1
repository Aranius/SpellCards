 = [Reflection.Assembly]::LoadFrom('C:\Users\arani\.nuget\packages\questpdf\2025.12.0\lib\net8.0\QuestPDF.dll')
.GetTypes() | Where-Object { .FullName -like '*Text*' } | Select-Object -First 50 -Property FullName
