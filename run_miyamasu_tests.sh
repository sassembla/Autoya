UNITY_APP=/Applications/Unity5.5.0p3/Unity.app/Contents/MacOS/Unity
echo using Unity @ ${UNITY_APP}

# update defines.
# echo -define:CLOUDBUILD > ./Assets/gmcs.rsp

# # update date.
# DATE=`date +%Y-%m-%d:%H:%M:%S`
# echo //${DATE} > ./Assets/MiyamasuTestRunner/Editor/Timestamp.cs

${UNITY_APP} -batchmode -projectPath $(pwd) -executeMethod Miyamasu.MiyamasuTestIgniter.RunBatchMode


# rm ./Assets/gmcs.rsp
# rm ./Assets/MiyamasuTestRunner/Editor/Timestamp.cs