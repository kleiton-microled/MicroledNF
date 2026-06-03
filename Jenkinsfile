pipeline {
    agent any

    parameters {
        booleanParam(name: 'BUILD_LOCAL_AGENT_PACKAGE', defaultValue: false, description: 'Publicar pacote win-x64 do LocalAgent com appsettings.Client.json')
        string(name: 'LOCAL_AGENT_CLIENT_CONFIG', defaultValue: 'deploy/clients/microled.example.json', description: 'Caminho do JSON do cliente (deploy/clients/*.json)')
    }

    environment {
        DOTNET_ROOT = "/usr/local/share/dotnet"
        PATH = "/usr/local/share/dotnet:/Users/macbook/.dotnet/tools:/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin:/usr/sbin:/sbin"
        PROJECT_PATH = "Microled.Nfe.Service.Api/Microled.Nfe.Service.Api.csproj"
        PUBLISH_DIR = "publish"
        LOCAL_AGENT_PROJECT = "Microled.Nfe.LocalAgent.Api/Microled.Nfe.LocalAgent.Api.csproj"
        VPS_HOST = "147.93.15.250"
        VPS_USER = "amktech"
        VPS_APP_DIR = "/var/www/amktechsistemas/notafiscal-api"
        SERVICE_NAME = "notafiscal-api"
    }

    stages {
        stage('Checkout') {
            steps {
                cleanWs()
                checkout scm
                sh 'echo "== COMMIT BUILDADO ==" && git log --oneline -3'
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

                    echo "== ARQUIVOS PUBLICADOS =="
                    ls -lh ${PUBLISH_DIR}/*.dll | head -5
                    echo "Build timestamp: $(date)"
                '''
            }
        }

        stage('Build LocalAgent Client Package') {
            when {
                expression { return params.BUILD_LOCAL_AGENT_PACKAGE }
            }
            steps {
                sh '''
                    echo "== LOCAL AGENT CLIENT PACKAGE =="
                    if command -v pwsh >/dev/null 2>&1; then
                      pwsh -File scripts/Prepare-ClientPackage.ps1 -ClientConfigPath "${LOCAL_AGENT_CLIENT_CONFIG}"
                    else
                      echo "pwsh not found; publishing win-x64 only (no appsettings.Client.json injection)"
                      dotnet publish "${LOCAL_AGENT_PROJECT}" -c Release -r win-x64 --self-contained true -o dist/localagent-publish/manual
                    fi
                '''
                archiveArtifacts artifacts: 'dist/localagent-publish/**/*', fingerprint: true, allowEmptyArchive: true
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

                        echo "== START DATABASE =="
                        ssh -o StrictHostKeyChecking=no ${VPS_USER}@${VPS_HOST} "cd ${VPS_APP_DIR} && docker compose up -d"

                        echo "== WAIT FOR DATABASE =="
                        ssh -o StrictHostKeyChecking=no ${VPS_USER}@${VPS_HOST} "until docker exec microled-nfe-postgres pg_isready -U amktech; do sleep 2; done"

                        echo "== RESTART SERVICE =="
                        ssh -o StrictHostKeyChecking=no ${VPS_USER}@${VPS_HOST} "sudo /usr/bin/systemctl restart ${SERVICE_NAME} && sudo /usr/bin/systemctl status ${SERVICE_NAME} --no-pager"
                    '''
                }
            }
        }
    }
}