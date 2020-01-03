

# monolith-to-microservice-demo


A demo project to demonstrate monolith to micro-service migration


## Docker commands

### Product service

Build
```
docker build -t moimhossain/product-demo:latest .
```

Run 
```
docker run -p 80:80 --env devday2020saname=<NAME> --env devday2020sakey=<KEY> moimhossain/product-demo:latest
```

### Sales Order service

Build
```
docker build -t moimhossain/salesorder-demo:latest .
```

Run 
```
docker run -p 80:80 --env devday2020saname=<NAME> --env devday2020sakey=<KEY>  moimhossain/salesorder-demo:latest
```