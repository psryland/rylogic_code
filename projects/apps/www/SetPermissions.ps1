$path = "D:\ZDrive\www\rylogic.co.nz"

Write-Host "Setting permissions for $path"

if (-Not (Test-Path $path)) {
    Write-Error "The path $path does not exist."
    exit 1
}

# Define the Read & Execute permission
$permission = [System.Security.AccessControl.FileSystemRights]::ReadAndExecute

# Get the folder's current ACL
$acl = Get-Acl $path

# Create a new access rule for the specified user
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IUSR", $permission, "ContainerInherit, ObjectInherit", "None", "Allow")

# Add the access rule to the ACL
$acl.SetAccessRule($accessRule)

# Create a new access rule for the specified user
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", $permission, "ContainerInherit, ObjectInherit", "None", "Allow")

# Add the access rule to the ACL
$acl.SetAccessRule($accessRule)

# Apply the updated ACL to the folder
Set-Acl $path $acl

Write-Output "Permissions set successfully for $path"
