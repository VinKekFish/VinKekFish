# /bin/bash
# sudo bash install.sh
# Виноградов Сергей Васильевич
# Поставки программы в стиле "как есть", без гарантий и ответственности. Используйте на свой страх и риск

# Директория, в которую будет установлен VinKekFish (vkf)
vkfDir=$1
# Полный путь к архиву с VinKekFish
arcDir=$2



user=`whoami`
if [[ $user != 'root' ]]
then
    echo -e '\033[41mWarning: install.sh not executed under root (executed under '$user'). Example:\033[0m\nsudo bash install.sh'
    # exit 1
fi


mkdir -p "$vkfDir"
ls "$vkfDir" &>> /dev/null
if [[ $? -ne 0 ]]
then
    echo -e "Program directory '$vkfDir' not found. The script executed by sudo?"
    exit 2
fi

ls "$arcDir" &>> /dev/null
if [[ $? -ne 0 ]]
then
    pathToArc=`realpath $0`
    echo -e "Archive '$arcDir' with VinKekFish was not found. Please, change '$pathToArc' file."
    exit 3
fi

echo -e "Program directory '$vkfDir' created or has been exists."

chmod a-rwx "$vkfDir"
chmod u+rwX "$vkfDir"
chmod a+rX  "$vkfDir"

cd "$vkfDir"

setfacl -d -m u::rwX .
setfacl -d -m g::rX  .
setfacl -d -m o::--- .

rm -rf exe
7z x -y -bb0 "$arcDir" >> /dev/null

chmod -R a-rwx exe
chmod -R a+rX  exe
chmod -R a+rx  exe/vkf


mkdir -p options
mkdir -p data

systemctl stop vkf
exe/vkf install
