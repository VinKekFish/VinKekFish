#!/bin/bash
# sudo bash install.sh
# Виноградов Сергей Васильевич
# Поставки программы в стиле "как есть", без гарантий и ответственности. Используйте на свой страх и риск

# Директория, в которую будет установлен VinKekFish (vkf)
vkfDir=$1
# Полный путь к архиву с VinKekFish
arcDir=$2

echo
date
echo "Install to $vkfDir (ru: Инсталляция в $vkfDir)"
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

if systemctl is-enabled --quiet vkf; then
    systemctl disable vkf
else
    systemctl disable vkf 2>/dev/null
fi

if systemctl is-active --quiet vkf; then
    systemctl stop vkf
else
    systemctl stop vkf 2>/dev/null
fi


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
#7z x -y -bb0 "$arcDir" >> /dev/null
cp -Rf $arcDir/exe .


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



tput civis
timeout=180  # Таймаут в секундах (180 с = 3 минуты)
elapsed=0    # Прошедшее время
interval=1   # Интервал проверки (1 секунда)
el=""

echo
echo
yes '-' | head -n ${COLUMNS:-$(tput cols)} | tr -d '\n'
echo 'Press Enter to make the installer wait for you to change service.options (see build.md)'
echo 'ru: Нажмите Enter, чтобы установщик подождал, пока вы поменяете service.options (см. build.md).'
while [ $elapsed -lt $timeout ]
do
    ((elapsed += interval))

    if [[ "$el" == "" ]]
    then
        el="."
        printf '\r\e[K'
        echo -n ''
    else
        el=""
        printf '\r\e[K'
        echo -n '>>> PRESS ENTER ONE OR TWO TIME <<<'
    fi

    tput cr
    
    if read -t $interval response; then

        echo "Please change /opt/VinKekFish/options/service.options file and press Enter for continue the installation."
        echo "ru: Пожалуйста, измените файл /opt/VinKekFish/options/service.options и нажмите Enter для продолжения установки."
        echo ">>> Installation is waiting <<<"

        read
        break

    fi
done
tput cnorm

echo "Continue the installation"
echo "ru: Продолжаем установку"

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
