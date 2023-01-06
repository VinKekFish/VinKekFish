# Файл нужно запускать из корневой директории репозитория VinKekFish
configuration=$1

if [ -z "$configuration" ]
then
    configuration="Release"
fi

rootDir=`pwd`

mkdir -p ./build/builder

dotnet publish src/builder/builder/ --output ./build/builder -c $configuration --self-contained false --use-current-runtime true /p:PublishSingleFile=true

bash ./b/fbuild.sh $configuration
