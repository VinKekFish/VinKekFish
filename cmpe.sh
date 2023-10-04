diff log-KNe.log log-KN.log > ./diff.log
diff -y log-KNe.log log-KN.log -W 512  > ./diffy.log

cmp -b log-KNe.log log-KN.log > ./diffc.log

if [[ $? -ne 0 ]]
then
    kompare log-KNe.log log-KN.log & &> /dev/null
else
    echo 'all OK'
fi
