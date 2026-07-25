pipeline {
    agent any

    options {
        skipDefaultCheckout(true)
    }

    parameters {
        booleanParam(
            name: 'RUN_LOCAL_DEPLOY',
            defaultValue: false,
            description: 'Run local docker compose deployment after validation.'
        )
        string(
            name: 'DOCKER_CLI_BIN',
            defaultValue: 'C:\\Users\\zx328\\AppData\\Local\\Programs\\DockerDesktop\\resources\\bin',
            description: 'Directory that contains docker.exe on the Jenkins machine.'
        )
    }

    environment {
        DOTNET_CONFIGURATION = 'Release'
        DOCKER_DESKTOP_MACHINE_BIN = 'C:\\Program Files\\Docker\\Docker\\resources\\bin'
        DOCKER_DESKTOP_USER_BIN = "${env.LOCALAPPDATA}\\Programs\\DockerDesktop\\resources\\bin"
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Show Tool Versions') {
            steps {
                withEnv(["PATH=${params.DOCKER_CLI_BIN};${env.DOCKER_DESKTOP_MACHINE_BIN};${env.DOCKER_DESKTOP_USER_BIN};${env.PATH}"]) {
                    powershell '''
                        dotnet --version
                        docker --version
                        docker compose version
                    '''
                }
            }
        }

        stage('Restore') {
            steps {
                powershell 'dotnet restore .\\FoodLedger.slnx'
            }
        }

        stage('Build') {
            steps {
                powershell 'dotnet build .\\FoodLedger.slnx --configuration $env:DOTNET_CONFIGURATION --no-restore'
            }
        }

        stage('Test') {
            steps {
                powershell 'dotnet test .\\FoodLedger.Tests\\FoodLedger.Tests.csproj --configuration $env:DOTNET_CONFIGURATION --no-build'
            }
        }

        stage('Validate Docker Compose') {
            steps {
                withEnv([
                    "PATH=${params.DOCKER_CLI_BIN};${env.DOCKER_DESKTOP_MACHINE_BIN};${env.DOCKER_DESKTOP_USER_BIN};${env.PATH}",
                    'POSTGRES_DB=Foodledger',
                    'POSTGRES_USER=postgres',
                    'POSTGRES_PASSWORD=jenkins-ci-only-password',
                    'POSTGRES_HOST_PORT=5432',
                    'FOODLEDGER_API_HTTP_PORT=5062'
                ]) {
                    powershell 'docker compose config --quiet'
                }
            }
        }

        stage('Local Deploy') {
            when {
                expression {
                    return params.RUN_LOCAL_DEPLOY
                }
            }
            steps {
                withEnv(["PATH=${params.DOCKER_CLI_BIN};${env.DOCKER_DESKTOP_MACHINE_BIN};${env.DOCKER_DESKTOP_USER_BIN};${env.PATH}"]) {
                    powershell '.\\scripts\\deploy-local.ps1'
                }
            }
        }
    }

    post {
        success {
            echo 'Jenkins pipeline completed successfully.'
        }
        failure {
            echo 'Jenkins pipeline failed. Check the stage logs for details.'
        }
    }
}
