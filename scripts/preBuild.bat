echo %date% %time%  > $(SolutionDir)\preBuild.txt

set __SUPPLEMENTAL_SOURCE=VHF_SUPPLEMENTAL_SOURCE_FILES
echo __SUPPLEMENTAL_SOURCE::%__SUPPLEMENTAL_SOURCE% >> $(SolutionDir)\preBuild.txt

chdir $(SolutionDir)
chdir ..
set __CURRENT_DIR=%CD%
echo __CURRENT_DIR::%__CURRENT_DIR% >> $(SolutionDir)\preBuild.txt

set __COMPONENTART_LIC=%__CURRENT_DIR%\%__SUPPLEMENTAL_SOURCE%\ComponentArt.Win.DataVisualization.lic
echo __COMPONENTART_LIC::%__COMPONENTART_LIC% >> $(SolutionDir)\preBuild.txt
set __COMPONENTART_LIC_DESTINATION=$(SolutionDir)\ContactTracing.CaseView >> $(SolutionDir)\preBuild.txt
xcopy /s /v /y /f %__COMPONENTART_LIC% $(SolutionDir)\ContactTracing.CaseView >> $(SolutionDir)\preBuild.txt

rem call $(SolutionDir)\preBuild.txt