## Setup Guide
Things you need to setup on [GCP](https://console.cloud.google.com) to deploy the IEA in case they aren't already configured.

### Building and pushing a Docker image to Artifact Registry 
1. Build: `docker build -t <image-name> .`
2. Tag: `docker image tag <image-name>:latest europe-west4-docker.pkg.dev/island-algorithm-infra/islands/island:latest`
3. Push: `docker push europe-west4-docker.pkg.dev/island-algorithm-infra/islands/island:latest`

    Note: this will containerize the latest version of the IEA algorithm and push it to Artifact Registry from where Kubernetes can access the image. 

### Kubernetes Cluster
1. [Install kubectl](https://kubernetes.io/docs/tasks/tools/)
2. [Install gcloud cli tool](https://cloud.google.com/sdk/docs/install)
3. Connect to the cluster: `gcloud container clusters get-credentials islands --region europe-west4 --project island-algorithm-infra`
4. Create an `islands` namespace: `k create namespace inslands`

### MongoDB
1. [Install helm](https://helm.sh/docs/intro/install/)
2. Fill in mongo-users.yaml with users and their corresponding passwords for the MongoDB instance.
2. Install MongoDB with helm: `helm install mongo oci://registry-1.docker.io/bitnamicharts/mongodb -n islands --values mongo-users.yaml`
3. Expose MongoDB through a LoadBalancer: `k apply -f mongo-service.yaml -n islands`
4. Check the assigned IP on [Google Cloud](https://console.cloud.google.com/net-services/loadbalancing/list/loadBalancers) and substitute in as `<load-balancer-ip>` in `MONGODB_URI`

### Islands
1. Setup a virtual environment for the notebook: `python -m venv .venv`
2. Run the following command to select the local environment:
    * Linux/MacOS: `source .venv/bin/activate`
    * Windows: `.venv/Scripts/activate`
3. Install dependencies: `pip install -r requirements.txt`
4. Open control-panel.ipynb and follow the instructions inside.
