# Azure AD B2C audit logs and reporting
This sample app demonstrate how to:
* **Manage users** - Such as export users, search a specific user, delete users and more. For more information, see [Azure AD B2C: Use the Azure AD Graph API](https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-devquickstarts-graph-dotnet)

* **Export the auditing log** - Azure Active Directory B2C (Azure AD B2C) emits audit logs containing activity information about B2C resources, issued tokens, and administrator access. This sample app demonstrates how to access this data for your Azure AD B2C tenant, and export the data to the file system. 
For more information, see: [Accessing Azure AD B2C audit logs](https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-reference-audit-logs) and [Accessing usage reports in Azure AD B2C via the reporting API](https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-reference-usage-reporting-api)


## Disclaimer
This sample app is developed and maintained by the open-source community in GitHub. The application is not part of Azure AD B2C product and it's not supported under any Microsoft standard support program or service. 
This sample app is provided AS IS without warranty of any kind.


## Register your application in your tenant
To communicate with the Graph API, you first need to have service account with administrative privileges. In Azure AD, you can do this by registering an application and authenticating to Azure AD. The application credentials are: **Application ID** and **Application Secret**. The application acts as itself, not as a user, to call the Graph API.

In this step, you register your Graph API application in Azure AD. Create application key (Application secret) and set the application with right privileges (write, set password, and delete users).

1. Sign in to the [Azure portal](https://portal.azure.com/).
2. Choose your Azure AD B2C tenant by selecting your account in the top right corner of the page.
3. From left panel, open the **Azure Active Directory** (not the Azure AD B2C). You might need to select **More Services** to find it.
4. Select **App registrations**.
5. Select **New application registration**.
6. Follow the prompts and create a new application
    * For **Name**, use **B2CUserMigratioin**.
    * For **Application type**, use **Web app/API**.
    * For **Sign-on URL**, use https://localhost (as it's not relevant for this application).
    * Click **Create**
7. Once it is created, select the newly created application **B2CUserMigratioin**.
Select **Properties**, copy the **Application ID**, and save it for later.

### Create application secret
8. Click on **Keys** and add a new key (also known as client secret). Also, copy the key for later.


### Grant administrative permission to your application
1. Continuing in the Azure portal's **Registered App**
2. Click on **Required permissions**.
3. Click on **Windows Azure Active Directory**.
4. In the **Enable Access**, under **Application Permissions**, select the **Read and write directory data permission** and click **Save**.
4. Finally, back in the **Required permissions**, click on the **Grant Permissions** button.

You now have an application that has permission to create, read, and update users from your B2C tenant.

### Configure User Account Administrator permissions for your application
Read and write directory data permission does NOT include the ability to delete users, or change their password. If you want to give your application the ability to delete users, or change password, you need to do these extra steps that involve PowerShell to set __User Account Administrator__ permissions, otherwise, you can skip to the next section.


> **Important**, You need to use a B2C tenant administrator account that is **local** to the B2C tenant. These accounts look like: admin@contosob2c.onmicrosoft.com.
>

Before you can run the cmdlets discussed in this article, you must first connect to your online service. To do so, run the cmdlet `Connect-AzureAD` at the Windows PowerShell command prompt. You will then be prompted for your credentials. For more information, see [Azure Active Directory PowerShell Version 2](https://docs.microsoft.com/en-us/powershell/azure/active-directory/install-adv2?view=azureadps-2.0).

```PowerShell
Connect-AzureAD
```

Use the **Application ID** in the following script to assign the application the user account administrator role, which allows to delete users. These roles have well-known identifiers, so all you need to do is input your **Application ID** in the script.

```PowerShell
$AppId = "<Your application ID>"

# Fetch Azure AD application to assign to role
$roleMember = Get-AzureADServicePrincipal -Filter "AppId eq '$AppId'"

# Fetch User Account Administrator role instance
$role = Get-AzureADDirectoryRole | Where-Object {$_.displayName -eq 'User Account Administrator'}

# If role instance does not exist, instantiate it based on the role template
if ($role -eq $null) {
    # Instantiate an instance of the role template
    $roleTemplate = Get-AzureADDirectoryRoleTemplate | Where-Object {$_.displayName -eq 'User Account Administrator'}
    Enable-AzureADDirectoryRole -RoleTemplateId $roleTemplate.ObjectId

    # Fetch User Account Administrator role instance again
    $role = Get-AzureADDirectoryRole | Where-Object {$_.displayName -eq 'User Account Administrator'}
}

# Add application to role
Add-AzureADDirectoryRoleMember -ObjectId $role.ObjectId -RefObjectId $roleMember.ObjectId

# Fetch role membership for role to confirm
Get-AzureADDirectoryRoleMember -ObjectId $role.ObjectId
```

Change the `$AppId` value with your Azure AD **Application ID**


## Application Settings
To test the sample solution, open the `AADB2C.GraphApi.sln` Visual Studio solution in Visual Studio. In the `AADB2C.GraphApi` project, open the `appsettings.json`. Replace the app settings with your own values:
* **PageSize**: Set the maximum number of returned objects for a request. When exceeded, the app pages forward in the Graph.
* **OutputFolder**: Output directory path. Empty string points to the current folder
* **Tenant**: Your Azure AD B2C tenant name or tenant Id
* **ClientId**: Your Azure AD Graph app ID
* **ClientSecret**: Your Azure AD Graph app secret
* **GraphApiVersion**: The Graph API version (default is 1.6) 

For example:

```JSON
{
  "AppSettings": {
    "PageSize": 500,
    "OutputFolder": "",
    "Tenant": "contoso.onmicrosoft.com",
    "ClientId": "2fd05e90-0fe3-4d80-8427-c776d8042477",
    "ClientSecret": "CIYVZC3WdpNypCrK0WK2DiXZwD6AmfGkYcea4caHWjg=",
    "GraphApiVersion": "1.6"
  }
```
 
## To run the solution
1) Run the console app
2) Select one of the commands
3) If nessacery, provide additional information
4) Check the result on the screen. For reporting and exporting data, open the output folder to see the result


