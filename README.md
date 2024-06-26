# microservices-in-a-world-of-containers dotnet

## 1 Create API which outputs the current Machinename

Using Visual Studio
- Create a new solution
- Create a ASP.NET Core Web Application
  - Disable HTTPS
  - Enable Docker Support
  - Enable OpenAPi

Using Command line:
- dotnet new webapi --use-controllers -o myWebApp --no-https
- Enable docker: https://learn.microsoft.com/en-us/dotnet/core/docker/build-container?tabs=windows&pivots=dotnet-8-0#create-the-dockerfile

Using Git and getting a default Visual Studio Solution
- git clone https://github.com/microservices-in-a-world-of-containers/dotnet.git

### Add controller
Now add a new controller called Environment. Either create file similar to existing controller or use VS to create it by right clicking on Controllers and clicking create new.
Setup a GET endpoint which returns the Machine name as a string, in the end it should look similar to below setup.

```C#
[Route("api/[controller]")]
[ApiController]
public class EnvironmentController : ControllerBase
{
    [HttpGet]
    public string Get()
    {
        return Environment.MachineName;
    }
}
```

Alternatively, in .NET8 or later, you can use the minimal API syntax, and add something similar to this in the `Program.cs`:
```C#
app.MapGet("/Environment", () =>
{
  return Environment.MachineName;
});
```

### Lets try to host it on docker

If using Visual Studio we can run a container using the built in run button if we have enabled docker support.
We want to try to do it manually from the commandline.

I have called my API `api` you can call yours whatever you want.
Build your docker image (replace imagename and imagetag, with name of image and version): 

`docker build -t imagename:imagetag -f .\Api\Dockerfile .`

`docker build -t api:1 -f .\Api\Dockerfile .`

Run your image and map your localhost port 80/81 (or whatever port you prefer) to port 80 (or the port your api uses) in the container:

`docker run -p 80:80 api:1`

`docker run -p 81:80 api:1`

Show your running docker containers `docker ps`

Stop a container `docker stop XXXXXX`

More docker commands: https://docs.docker.com/get-started/docker_cheatsheet.pdf

When testing remember to add your path:
- If using similar to WeatherForecast: http://localhost/Environment
- If using VS "Add Controller": http://localhost/api/Environment

Port 81:
http://localhost:81/Environment
http://localhost:81/Api/Environment


## 2 Install Kubernetes Dashboard
Have you enabled Kubernetes in Docker?
Now we are ready to play around with some Kubernetes, lets see if we can get the Dashboard up and running first so we have some visual stuff and not just a CLI.

Official doc: https://github.com/kubernetes/dashboard

Now create an account that can access the dashboard.

Run `kubectl apply -f https://raw.githubusercontent.com/microservices-in-a-world-of-containers/dotnet/main/Dashboard/sa.yaml` 
This will create a service account and give it cluster-admin access
Run the following to get a token you can use for logging into the dashboard `kubectl -n kubernetes-dashboard create token admin-user`
Access dashboard and use token.

You should now be logged in and be able to navigate the dashboard.

## 3 Deploy API to Kubernetes
Now try to add the created docker image to Kubernetes, the `kubectl.yaml` contains the yaml code needed for creating a deployment and a service in kubernetes.
deployment: https://kubernetes.io/docs/concepts/workloads/controllers/deployment/#creating-a-deployment
service: https://kubernetes.io/docs/concepts/services-networking/service/#defining-a-service

For the service add the `type: Loadbalancer` as seen below
```yaml
apiVersion: v1
kind: Service
metadata:
  name: api-service
spec:
  selector:
    app: api
  ports:
    - protocol: TCP
      port: 80
      targetPort: 8080
  type: LoadBalancer
```
Run `kubectl apply -f api.yaml` to apply it to kubernetes.
The service type is of the type LoadBalancer, which means it tries to expose it to your localhost if the service should only be exposed internally use ClusterIP instead.

imagePullPolicy have been set to IfNotPresent, therefore kubernetes tries to pull from your local docker repository. If this was not set it would try to pull it from a remote repository.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-deployment
  labels:
    app: api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: api
  template:
    metadata:
      labels:
        app: api
    spec:
      containers:
      - name: api
        image: api:1
        **imagePullPolicy: IfNotPresent**
        ports:
        - containerPort: 8080
```

### Access the different pods

Try to access the API and see the Machinename.
Now try to scale up more pods of the same container, this can be done in the yaml file by changing the replicas setting and then running kubetl apply again or by using the dashboard. Test to see if you can hit different pods (Hint, try curl, since Chrome does some binding/caching).


## 4 Create new API invoking the old API
I have called this API `api-invoke`
Create a new API which points towards the old API, localhost you can use localhost/Environment, but when hosted in kubernetes you should use the internal dns, in my example it is http://api-service:80/Environment.
You can lookup the dns using the dashboard if you go the page called services.
Remember to create a new Kubernetes yaml file, and give it another name.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-invoke-deployment
  labels:
    app: api-invoke
spec:
  replicas: 1
  selector:
    matchLabels:
      app: api-invoke
  template:
    metadata:
      labels:
        app: api-invoke
    spec:
      containers:
      - name: api-invoke
        image: api-invoke:1
        imagePullPolicy: IfNotPresent
        ports:
        - containerPort: 8080
---
apiVersion: v1
kind: Service
metadata:
  name: api-invoke-service
spec:
  selector:
    app: api-invoke
  ports:
    - protocol: TCP
      port: 81
      targetPort: 8080
  type: LoadBalancer
```

To see if it works delete the old service for your old api, and then re-create it using a ClusterIP. ClusterIP only lets pods internally in Kubernetes access the service. OBS: you need to delete the existing service before the tpye can be changed to ClusterIP.


## 5 Create new API storing data in a file
I have called this API `file-store`
Create a simple api again this time the controller should be able to write to a file and get the current file text.

```C#
private const string FileName = "/tmp/text/test.txt";

[HttpGet]
public string Get()
{
    if (!System.IO.File.Exists(FileName)) return "";
    using var sr = System.IO.File.OpenText(FileName);
    return sr.ReadToEnd();
}

[HttpPost]
public IActionResult Post(string input)
{
    using var streamWriter = System.IO.File.CreateText(FileName);
    streamWriter.WriteLine(input);
    return Ok();
}
```

This is relativly easy if you have completed the other tasks, but the problem comes when you host it in docker/kubernetes and the pod restarts or dies then you lose your data.
Lets fix that!!!
By using persistent storage. Create a PersistentVolumeClaim and map use it in your pod, and place the file in mapped folder. All pods for that deployment will then use this storage.

pvc doc: https://kubernetes.io/docs/tasks/configure-pod-container/configure-persistent-volume-storage/#create-a-persistentvolumeclaim

How to use pvc in pods: https://kubernetes.io/docs/tasks/configure-pod-container/configure-persistent-volume-storage/#create-a-pod

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: file-store-deployment
  labels:
    app: file-store
spec:
  replicas: 1
  selector:
    matchLabels:
      app: file-store
  template:
    metadata:
      labels:
        app: file-store
    spec:
      containers:
      - name: file-store
        image: file-store:1
        imagePullPolicy: IfNotPresent
        ports:
        - containerPort: 8080
        volumeMounts:
        - mountPath: "/tmp/text"
          name: file-store-pv-storage
      volumes:
      - name: file-store-pv-storage
        persistentVolumeClaim:
          claimName: file-store-pv-claim
---
apiVersion: v1
kind: Service
metadata:
  name: file-store-service
spec:
  selector:
    app: file-store
  ports:
    - protocol: TCP
      port: 82
      targetPort: 8080
  type: LoadBalancer
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: file-store-pv-claim
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 3Gi
```

## Extra stuff
You managed to get this far!!!
Now it is time to make something crazy using your knowledge.
- Create a Calculator where each functionality like add, subtract, multiply is stored in its own microservice and then a calculator service which uses these services.
- Create a logging service where other services can log to, so you have all your logs together.
- Setup a RabbitMQ setup in your Kubernetes Cluster and use it in a service.
- Setup a Prometheus service that collects metrics from your service.
- Find something even crazier to create only the imagination set the limits.
