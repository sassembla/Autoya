UNITY_APP=/Applications/Unity5.6.1p4/Unity.app/Contents/MacOS/Unity
echo using Unity @ ${UNITY_APP}

${UNITY_APP} -batchmode -projectPath $(pwd) -quit -executeMethod AutoyaFramework.BuildEntryPoint.Entry -m "herecomes!"