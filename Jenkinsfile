pipeline {
    agent any

    environment {
        DOTNET_ROOT = "/usr/local/share/dotnet"
        PATH = "/usr/local/share/dotnet:/Users/macbook/.dotnet/tools:/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin:/usr/sbin:/sbin"
        PROJECT_PATH = "Microled.Nfe.Service.Api/Microled.Nfe.Service.Api.csproj"
        INFRA_PROJECT_PATH = "Microled.Nfe.Service.Infra/Microled.Nfe.Service.Infra.csproj"
        PUBLISH_DIR = "publish"
        VPS_HOST = "147.93.15.250"
        VPS_USER = "amktech"
        VPS_APP_DIR = "/var/www/amktechsistemas/notafiscal-api"
        SERVICE_NAME = "notafiscal-api"
        DB_CONNECTION = "Host=127.0.0.1;Port=5435;Database=DB_NFE;Username=amktech;Password=Index@!1212"
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build and Publish') {
            steps {
                sh '''
                    echo "== DOTNET INFO =="
                    dotnet --info

                    echo "== CLEAN PUBLISH DIR =="
                    rm -rf ${PUBLISH_DIR}
                    mkdir -p ${PUBLISH_DIR}

                    echo "== RESTORE =="
                    dotnet restore ${PROJECT_PATH}

                    echo "== PUBLISH =="
                    dotnet publish ${PROJECT_PATH} -c Release -o ${PUBLISH_DIR}

                    echo "== BUILD MIGRATIONS BUNDLE =="
                    dotnet tool install --global dotnet-ef || true
                    dotnet restore --runtime linux-x64
                    dotnet ef migrations bundle \
                        --project ${INFRA_PROJECT_PATH} \
                        --startup-project ${PROJECT_PATH} \
                        --runtime linux-x64 \
                        --self-contained \
                        -o efbundle \
                        --force
                '''
            }
        }

        stage('Deploy to VPS') {
            steps {
                sshagent(credentials: ['vps-amktech-ssh']) {
                    sh '''
                        echo "== CREATE REMOTE DIR =="
                        ssh -o StrictHostKeyChecking=no ${VPS_USER}@${VPS_HOST} "mkdir -p ${VPS_APP_DIR}"

                        echo "== COPY FILES =="
                        scp -o StrictHostKeyChecking=no -r ${PUBLISH_DIR}/* ${VPS_USER}@${VPS_HOST}:${VPS_APP_DIR}/

                        echo "== COPY DOCKER COMPOSE =="
                        scp -o StrictHostKeyChecking=no docker-compose.yml ${VPS_USER}@${VPS_HOST}:${VPS_APP_DIR}/

                        echo "== COPY MIGRATIONS BUNDLE =="
                        scp -o StrictHostKeyChecking=no efbundle ${VPS_USER}@${VPS_HOST}:${VPS_APP_DIR}/

                        echo "== START DATABASE =="
                        ssh -o StrictHostKeyChecking=no ${VPS_USER}@${VPS_HOST} "cd ${VPS_APP_DIR} && docker compose up -d"

                        echo "== WAIT FOR DATABASE =="
                        ssh -o StrictHostKeyChecking=no ${VPS_USER}@${VPS_HOST} "until docker exec microled-nfe-postgres pg_isready -U amktech; do sleep 2; done"

                        echo "== RUN MIGRATIONS =="
                        ssh -o StrictHostKeyChecking=no ${VPS_USER}@${VPS_HOST} "chmod +x ${VPS_APP_DIR}/efbundle && NFE_DB_CONNECTION='${DB_CONNECTION}' ${VPS_APP_DIR}/efbundle"

                        echo "== RESTART SERVICE =="
                        ssh -o StrictHostKeyChecking=no ${VPS_USER}@${VPS_HOST} "sudo /usr/bin/systemctl restart ${SERVICE_NAME} && sudo /usr/bin/systemctl status ${SERVICE_NAME} --no-pager"
                    '''
                }
            }
        }
    }
}