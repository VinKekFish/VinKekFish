# /bin/bash
# sudo bash install.sh
# Виноградов Сергей Васильевич
# Поставки программы в стиле "как есть", без гарантий и ответственности. Используйте на свой страх и риск

# Директория, в которую будет установлен VinKekFish (vkf)
vkfDir=$1
# Полный путь к архиву с VinKekFish
arcDir=$2

echo
date
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

echo -e "\033[32mThe program directory '$vkfDir' created or has been exists. (ru: успешно создана или найдена существующая папка программы '$vkfDir')\033[0m"
echo -e "The installation continue... (ru: установка продолжается...)"

chmod a-rwx "$vkfDir"
chmod u+rwX "$vkfDir"
chmod a+rX  "$vkfDir"

cd "$vkfDir"

echo
date
echo "Wait for stop the VinKekFish service (vkf service), if executed. This may take 1 minute."
echo "ru: Ждём остановки сервиса VinKekFish (сервис vkf), если запущено. Это может занять 1 минуту."
echo

systemctl disable vkf
systemctl stop vkf

echo "If vkf is used to mount disks, the disks will be unmounted (ru: Если vkf используется для монтирования дисков, диски будут размонтированы)"

pidof -q vkf
while [[ $? -eq 0 ]]
do
    echo
    date
    echo 'Waiting for the end of processes (ru: Ожидаем завершения процессов) [see pidvkf=`pidof vkf`; kill $pidvkf]'
    pidvkf=`pidof vkf`
    ps h -o pid,user,cmd --pid $pidvkf
    # killall -s SIGINT -q vkf
    killall -q vkf
    sleep 8
    pidof -q vkf
done

echo
echo "Stoppped (ru: Остановлено)"
echo


# Выполняем копирование
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


# Выполняем основную установку
exe/vkf install

if [[ $? -ne 0 ]]
then
    echo; echo;
    echo -e "\033[41mThe vkf program installation is unsuccessfully ended (error in vkf install) (ru: произошла ошибка при попытке установки программы vkf)\033[0m"
    echo
    echo "see logs by command"
    echo "sudo journalctl -u vkf -e"
    echo; echo;
    exit 1
fi


# Дадим пользователю хоть краем глаза взглянуть на то, что было выведено до этого
sleep 3
systemctl start vkf

sleep 3

echo
echo 'INFORMATION: '

echo
vkf version
echo
echo
sleep 3

systemctl status -l --no-pager vkf > /dev/null
if [[ $? -ne 0 ]]
then
    echo; echo;
    echo -e "\033[41mThe vkf program installation is unsuccessfully ended (ru: произошла ошибка при попытке установки программы vkf)\033[0m"
    echo
    echo "see logs by command"
    echo "sudo journalctl -u vkf -e"
    echo; echo;
    exit 3
fi

echo; echo;
echo ------------------------------------------------
echo; echo;
echo -e "\033[32mThe vkf program is successfully installed (ru: программа успешно установлена)\033[0m"
echo
echo 'Example for get random bytes:'
echo 'ru: Пример получения случайных байтов от сервиса:'
echo cat /dev/vkf/crandom ">>" /some/Path/file.key
echo nc -UN /dev/vkf/random ">>" /some/Path/file.key
echo
echo
echo 'For reading log of service use commands:'
echo 'ru: Для чтения логов программы используйте следующие команды:'
echo sudo journalctl -e -u vkf
echo -n "sudo watch "
echo -ne \'
echo -n journalctl --no-pager -u vkf
echo -n " | "
echo -n tail -n 15
echo \'
echo

date
echo "Wait for start the VinKekFish service (vkf service). This may take 5-7 minutes."
echo "ru: Ждём запуска сервиса VinKekFish (сервис vkf). Это может занять 5-7 минут."
nc -UN /dev/vkf/random > /dev/null
echo; echo;
# journalctl -e -u vkf --no-pager | tail -n 1
echo 'Service status:'
systemctl status -l --no-pager vkf

echo; date;
echo;echo;
