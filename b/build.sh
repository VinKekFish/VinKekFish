# Файл нужно запускать из корневой директории репозитория VinKekFish
configuration=$1
testTags=$2

if [ -z "$configuration" ]
then
    configuration="Release"
fi

if [ -z "$testTags" ]
then
    testTags="fast fast_level2 +mandatory"
fi


rootDir=`pwd`

mkdir -p ./build/builder

dotnet publish src/builder/builder/ --output ./build/builder -c $configuration --self-contained false --use-current-runtime true /p:PublishSingleFile=false

bash ./b/fbuild.sh "$configuration" "$testTags"
