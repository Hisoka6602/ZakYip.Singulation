set serviceName=ZakYip.Singulation.Host

sc stop   %serviceName% 
sc delete %serviceName% 

pause