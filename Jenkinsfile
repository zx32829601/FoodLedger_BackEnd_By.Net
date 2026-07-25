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
    }

    environment {
        DOTNET_CONFIGURATION = 'Release'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Show Tool Versions') {
            steps {
                powershell '''
                    dotnet --version
                    docker --version
                    docker compose version
                '''
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
                powershell '.\\scripts\\deploy-local.ps1'
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
