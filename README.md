# Requirements engineering copilot

This is Reqcraft, a requirements engineering copilot that helps you write useful user stories with a focus on quality
and security. We're exploring this agent internally to write user stories. It's an experiment with
Semantic Kernel and .NET Aspire to find out if these tools are a good fit for us.

You're free to use this agent. We don't provide any support for it. If you run into issues, you can post your issues
here. We love to see pull requests for things that don't work.

## Documentation

### Deploying to Azure

We use Azure Container Apps to run the application. You can provision the application infrastructure by
running the following command:

```bash
azd provision
```

After provisioning the infrastructure, deploy the container apps using the following command:

```bash
azd deploy
```

### Updating the deployment after changing the code

You only need to run the `azd deploy` command to update the container images when you changed the code in the assistant app.
If you changed any code in the `host/ReqCraft.AppHost` project you'll need to run `azd provision` to update the provisioning.

**Note:** Make sure to update `apps/assistant/Reqcraft.Assistant/manifests/containerApp.tmpl.yaml` with new connection
strings and settings. The Azure Developer CLI generates this file only once.

## Troubleshooting

Q: I visited the service endpoint listed, and I'm seeing a blank or error page.

A: Your service may have failed to start or misconfigured. To investigate further:

1. Click on the resource group link shown to visit Azure Portal.
2. Navigate to the specific Azure Container App resource for the service.
3. Select _Monitoring -> Log stream_ under the navigation pane.
4. Observe the log output to identify any errors.
5. If logs are written to disk, examine the local logs or debug the application by using the _Console_
   to connect to a shell within the running container.

For additional information about setting up your `azd` project, visit the official
[docs](https://learn.microsoft.com/azure/developer/azure-developer-cli/make-azd-compatible?pivots=azd-convert).
