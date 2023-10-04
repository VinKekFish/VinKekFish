diff log-KN.log log-k1.log > ./diff.log
diff -y log-KN.log log-k1.log -W 512  > ./diffy.log

cmp -b log-KN.log log-k1.log > ./diffc.log

if [[ $? -ne 0 ]]
then
    kompare log-KN.log log-k1.log & &> /dev/null
else
    echo 'all OK'
fi
