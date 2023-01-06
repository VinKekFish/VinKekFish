# Файл нужно запускать из корневой директории репозитория VinKekFish
configuration=$1

if [ -z "$configuration" ]
then
    configuration="Release"
fi


./build/builder/builder $configuration
