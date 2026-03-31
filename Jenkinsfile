pipeline {
    agent any

    environment {
        DOTNET_ROOT = "/usr/local/share/dotnet"
        PATH = "/usr/local/share/dotnet:/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin:/usr/sbin:/sbin"
        PROJECT_PATH = "/Microled.Nfe.Service.Api/Microled.Nfe.Service.Api.csproj"
        PUBLISH_DIR = "publish"
        VPS_HOST = "147.93.15.250"
        VPS_USER = "root"
        VPS_APP_DIR = "/var/www/amktechsistemas/notafiscal-api"
        SERVICE_NAME = "notafiscal-api"
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
                '''
            }
        }

        stage('Deploy to VPS') {
            steps {
                sshagent(credentials: ['vps-root-ssh']) {
                    sh '''
                        echo "== CREATE REMOTE DIR =="
                        ssh -o StrictHostKeyChecking=no ${VPS_USER}@${VPS_HOST} "mkdir -p ${VPS_APP_DIR}"

                        echo "== COPY FILES =="
                        scp -o StrictHostKeyChecking=no -r ${PUBLISH_DIR}/* ${VPS_USER}@${VPS_HOST}:${VPS_APP_DIR}/

                        echo "== RESTART SERVICE =="
                        ssh -o StrictHostKeyChecking=no ${VPS_USER}@${VPS_HOST} "systemctl restart ${SERVICE_NAME} && systemctl status ${SERVICE_NAME} --no-pager"
                    '''
                }
            }
        }
    }
}