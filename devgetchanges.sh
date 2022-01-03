#!/bin/bash

cd ~

if [ -d "Documents/workspace/microting/eform-service-backend-configuration-plugin/ServiceBackendConfigurationPlugin" ]; then
	rm -fR Documents/workspace/microting/eform-service-backend-configuration-plugin/ServiceBackendConfigurationPlugin
fi

cp -av Documents/workspace/microting/eform-debian-service/Plugins/ServiceBackendConfigurationPlugin Documents/workspace/microting/eform-service-backend-configuration-plugin/ServiceBackendConfigurationPlugin
