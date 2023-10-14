# /bin/bash
# sudo bash install.sh
# Виноградов Сергей Васильевич
# Поставки программы в стиле "как есть", без гарантий и ответственности. Используйте на свой страх и риск

# Директория, в которую будет установлен VinKekFish (vkf)
vkfDir=$1
# Полный путь к архиву с VinKekFish
arcDir=$2


echo

user=`whoami`
if [[ $user != 'root' ]]
then
    echo -e '\033[41mError: install.sh not executed under root (executed under '$user'). Example:\033[0m\nsudo bash install.sh'
    exit 1
fi


mkdir -p "$vkfDir"
ls "$vkfDir" &>> /dev/null
if [[ $? -ne 0 ]]
then
    echo -e "Program directory '$vkfDir' not found. The script must be executed by sudo."
    exit 2
fi

ls "$arcDir" &>> /dev/null
if [[ $? -ne 0 ]]
then
    pathToArc=`realpath $0`
    echo -e "Archive '$arcDir' with VinKekFish was not found. Please, change '$pathToArc' file."
    exit 3
fi

echo -e "\033[32mThe program directory '$vkfDir' created or has been exists. (успешно создана папка программы)\033[0m"
echo -e "The installation continue... (установка продолжается...)"

chmod a-rwx "$vkfDir"
chmod u+rwX "$vkfDir"
chmod a+rX  "$vkfDir"

cd "$vkfDir"

setfacl -d -m u::rwX .
setfacl -d -m g::rX  .
setfacl -d -m o::--- .

rm -rf exe
7z x -y -bb0 "$arcDir" >> /dev/null

rm -f /usr/local/bin/vkf
ln -s "$vkfDir/exe/vkf" /usr/local/bin/vkf

chmod -R a-rwx exe
chmod -R a+rX  exe
chmod -R a+rx  exe/vkf


mkdir -p options
mkdir -p data

chmod -R o-rwx options
chmod -R o-rwx data

date
systemctl stop vkf
killall -s SIGINT -wq vkf

exe/vkf install

if [[ $? -ne 0 ]]
then
    echo; echo;
    echo -e "\033[41mThe vkf program installation is unsuccessfully ended (error in vkf install)\033[0m"
    echo
    exit 1
fi


systemctl start vkf

echo
echo 'INFORMATION: '

echo
vkf version
echo
echo
sleep 3

echo 'Service status:'
systemctl status vkf
if [[ $? -ne 0 ]]
then
    echo; echo;
    echo -e "\033[41mThe vkf program installation is unsuccessfully ended\033[0m"
    echo
    exit 3
fi


echo; echo;
echo -e "\033[32mThe vkf program is successfully installed (программа успешно установлена)\033[0m"
echo
echo 'Example for get random bytes: nc -UN /dev/vkf/random'
echo
