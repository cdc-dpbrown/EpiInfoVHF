echo %date% %time%  > $(SolutionDir)\postBuild.txt

set __PARING_SOURCE=VHF_Epi_Info_7_RELEASE_PAIRING_SOURCE
echo __PARING_SOURCE=%__PARING_SOURCE% >> $(SolutionDir)\postBuild.txt

set __VHF_BUILD=__VHF_BUILD
echo __VHF_BUILD=%__VHF_BUILD% >> $(SolutionDir)\postBuild.txt

chdir $(SolutionDir)
chdir ..
set __CURRENT_DIR=%CD%
echo __CURRENT_DIR=%__CURRENT_DIR% >> $(SolutionDir)\postBuild.txt

rmdir /s /q %__VHF_BUILD%
mkdir %__VHF_BUILD%

xcopy /s /v /y /f "%CURRENT_DIR%%__PARING_SOURCE%" "%CURRENT_DIR%%__VHF_BUILD%" >> "$(SolutionDir)\postBuild.txt"

xcopy /s /v /y /f "$(TargetDir)Epi Info VHF.exe" "%CURRENT_DIR%%__VHF_BUILD%" >> "$(SolutionDir)\postBuild.txt"

xcopy /s /v /y /f "$(TargetDir)CaseManagementMenu.exe" "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7" >> "$(SolutionDir)\postBuild.txt"
xcopy /s /v /y /f "$(TargetDir)ContactTracing.WPF.dll" "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7" >> "$(SolutionDir)\postBuild.txt"
xcopy /s /v /y /f "$(TargetDir)ContactTracing.ImportExport.dll" "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7" >> "$(SolutionDir)\postBuild.txt"
xcopy /s /v /y /f "$(TargetDir)ContactTracing.ViewModel.dll" "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7" >> "$(SolutionDir)\postBuild.txt"
xcopy /s /v /y /f "$(TargetDir)ContactTracing.Core.dll" "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7" >> "$(SolutionDir)\postBuild.txt"

mkdir "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7\en-US"
xcopy /s /v /y /f "$(TargetDir)\en-US" "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7\en-US" >> "$(SolutionDir)\postBuild.txt"

mkdir "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7\fr"
xcopy /s /v /y /f "$(TargetDir)\fr" "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7\fr" >> "$(SolutionDir)\postBuild.txt"

mkdir "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7\fr-FR"
xcopy /s /v /y /f "$(TargetDir)\fr-FR" "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7\fr-FR" >> "$(SolutionDir)\postBuild.txt"

mkdir "%__VHF_BUILD%\Epi Info 7\Projects\VHF"
xcopy /s /v /y /f "$(TargetDir)\Projects\VHF" "%CURRENT_DIR%%__VHF_BUILD%\Epi Info 7\Projects\VHF" >> "$(SolutionDir)\postBuild.txt"

del $(SolutionDir)\ContactTracing.CaseView\ComponentArt.Win.DataVisualization.lic >> "$(SolutionDir)\postBuild.txt"

rem call "$(SolutionDir)\postBuild.txt"