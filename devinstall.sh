#!/bin/bash

cd ~

if [ -d "Documents/workspace/microting/eform-debian-service/Plugins/ServiceBackendConfigurationPlugin" ]; then
	rm -fR Documents/workspace/microting/eform-debian-service/Plugins/ServiceBackendConfigurationPlugin
fi

mkdir Documents/workspace/microting/eform-debian-service/Plugins

cp -av Documents/workspace/microting/eform-service-backend-configuration-plugin/ServiceBackendConfigurationPlugin Documents/workspace/microting/eform-debian-service/Plugins/ServiceBackendConfigurationPlugin
