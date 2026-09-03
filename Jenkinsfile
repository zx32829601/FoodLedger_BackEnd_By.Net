pipeline {
    agent any

    options {
        skipDefaultCheckout(true)
    }

    triggers {
        pollSCM('H * * * *')
    }

    parameters {
        booleanParam(
            name: 'RUN_LOCAL_DEPLOY',
            defaultValue: true,
            description: 'Run local docker compose deployment after validation.'
        )
        string(
            name: 'DOCKER_CLI_BIN',
            defaultValue: 'C:\\Users\\zx328\\AppData\\Local\\Programs\\DockerDesktop\\resources\\bin',
            description: 'Directory that contains docker.exe on the Jenkins machine.'
        )
        string(
            name: 'ASPNETCORE_ENVIRONMENT',
            defaultValue: 'InternalTesting',
            description: 'ASP.NET Core environment. InternalTesting allows HTTP cookies only for isolated LAN testing.'
        )
        string(
            name: 'FOODLEDGER_CORS_ALLOWED_ORIGIN',
            defaultValue: '',
            description: 'Optional frontend origin override. Leave blank to use the deployment host .env value.'
        )
        booleanParam(
            name: 'APPLY_DATABASE_MIGRATIONS',
            defaultValue: true,
            description: 'Apply pending EF Core migrations when the API container starts.'
        )
        booleanParam(
            name: 'FORCE_DOCKER_REBUILD',
            defaultValue: false,
            description: 'Rebuild the API image without Docker build cache.'
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
                    powershell 'dotnet --version'
                    powershell 'docker --version'
                    powershell "& './scripts/Invoke-DockerCompose.ps1' -ArgumentList @('version')"
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
                    'POSTGRES_HOST_PORT=5433',
                    'FOODLEDGER_API_HTTP_PORT=5062',
                    "ASPNETCORE_ENVIRONMENT=${params.ASPNETCORE_ENVIRONMENT}",
                    "FOODLEDGER_CORS_ALLOWED_ORIGIN=${params.FOODLEDGER_CORS_ALLOWED_ORIGIN}",
                    "FOODLEDGER_APPLY_MIGRATIONS_ON_STARTUP=${params.APPLY_DATABASE_MIGRATIONS}"
                ]) {
                    powershell "& './scripts/Invoke-DockerCompose.ps1' -ArgumentList @('config', '--quiet')"
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
                script {
                    def deploymentEnvironment = [
                        "PATH=${params.DOCKER_CLI_BIN};${env.DOCKER_DESKTOP_MACHINE_BIN};${env.DOCKER_DESKTOP_USER_BIN};${env.PATH}",
                        "ASPNETCORE_ENVIRONMENT=${params.ASPNETCORE_ENVIRONMENT}",
                        "POSTGRES_HOST_PORT=5433",
                        "FOODLEDGER_APPLY_MIGRATIONS_ON_STARTUP=${params.APPLY_DATABASE_MIGRATIONS}"
                    ]

                    if (params.FOODLEDGER_CORS_ALLOWED_ORIGIN?.trim()) {
                        deploymentEnvironment.add(
                            "FOODLEDGER_CORS_ALLOWED_ORIGIN=${params.FOODLEDGER_CORS_ALLOWED_ORIGIN.trim()}"
                        )
                    }

                    def noCacheArgument = params.FORCE_DOCKER_REBUILD ? '-NoCache' : ''

                    withCredentials([
                        file(
                            credentialsId: 'FoodLedger.env',
                            variable: 'FOODLEDGER_ENV_FILE'
                        )
                    ]) {
                        powershell '''
                            Copy-Item `
                                -LiteralPath $env:FOODLEDGER_ENV_FILE `
                                -Destination (Join-Path $env:WORKSPACE '.env') `
                                -Force
                        '''

                        try {
                            timeout(time: 10, unit: 'MINUTES') {
                                withEnv(deploymentEnvironment) {
                                    powershell "& './scripts/deploy-local.ps1' -Pull ${noCacheArgument}"
                                }
                            }
                        }
                        finally {
                            powershell '''
                                $envPath = Join-Path $env:WORKSPACE '.env'

                                if (Test-Path -LiteralPath $envPath) {
                                    Remove-Item -LiteralPath $envPath -Force
                                }
                            '''
                        }
                    }
                }
            }
        }
    }

    post {
        success {
            echo 'Jenkins pipeline completed successfully.'
        }
        failure {
            script {
                withEnv(["PATH=${params.DOCKER_CLI_BIN};${env.DOCKER_DESKTOP_MACHINE_BIN};${env.DOCKER_DESKTOP_USER_BIN};${env.PATH}"]) {
                    powershell returnStatus: true, script: '''
                        & './scripts/Invoke-DockerCompose.ps1' -ArgumentList @(
                            'logs',
                            '--tail=200',
                            'foodledger-api'
                        )
                    '''
                }
            }
            echo 'Jenkins pipeline failed. Check the stage logs for details.'
        }
    }
}
