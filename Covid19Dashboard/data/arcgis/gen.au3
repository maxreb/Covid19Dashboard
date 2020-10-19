$sread = FileRead("20201018.json")
For $i = 10 To 24
   $filename = "0" & $i&".json"
   if not FileExists($filename) Then FileWrite($filename,$sread)
Next
