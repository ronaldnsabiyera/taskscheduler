﻿using System;
using System.Security.Principal;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Provides the methods that are used to register (create) tasks in the folder, remove tasks from the folder, and create or remove subfolders from the folder.
	/// </summary>
	public sealed class TaskFolder : IDisposable, IComparable<TaskFolder>
	{
		V1Interop.ITaskScheduler v1List = null;
		V2Interop.ITaskFolder v2Folder = null;

		internal TaskFolder(TaskService svc)
		{
			TaskService = svc;
			v1List = svc.v1TaskScheduler;
		}

		internal TaskFolder(TaskService svc, V2Interop.ITaskFolder iFldr)
		{
			TaskService = svc;
			v2Folder = iFldr;
		}

		/// <summary>
		/// Releases all resources used by this class.
		/// </summary>
		public void Dispose()
		{
			if (v2Folder != null)
				System.Runtime.InteropServices.Marshal.ReleaseComObject(v2Folder);
			v1List = null;
		}

		/// <summary>
		/// Gets a <see cref="System.Collections.Generic.IEnumerator{Task}"/> which enumerates all the tasks in this and all subfolders.
		/// </summary>
		/// <value>
		/// A <see cref="System.Collections.Generic.IEnumerator{Task}"/> for all <see cref="Task"/> instances.
		/// </value>
		public System.Collections.Generic.IEnumerable<Task> AllTasks => EnumerateFolderTasks(this);

		/// <summary>
		/// Gets the name that is used to identify the folder that contains a task.
		/// </summary>
		public string Name => (v2Folder == null) ? @"\" : v2Folder.Name;

		/// <summary>
		/// Gets the parent folder of this folder.
		/// </summary>
		/// <value>
		/// The parent folder, or <c>null</c> if this folder is the root folder.
		/// </value>
		public TaskFolder Parent
		{
			get
			{
				// V1 only has the root folder
				if (v2Folder == null)
					return null;

				string path = v2Folder.Path;
				string parentPath = System.IO.Path.GetDirectoryName(path);
				if (string.IsNullOrEmpty(parentPath))
					return null;
				return TaskService.GetFolder(parentPath);
			}
		}

		/// <summary>
		/// Gets the path to where the folder is stored.
		/// </summary>
		public string Path => (v2Folder == null) ? @"\" : v2Folder.Path;

		internal TaskFolder GetFolder(string Path)
		{
			if (v2Folder != null)
				return new TaskFolder(TaskService, v2Folder.GetFolder(Path));
			throw new NotV1SupportedException();
		}

		/// <summary>
		/// Gets or sets the security descriptor of the task.
		/// </summary>
		/// <value>The security descriptor.</value>
		[Obsolete("This property will be removed in deference to the GetAccessControl, GetSecurityDescriptorSddlForm, SetAccessControl and SetSecurityDescriptorSddlForm methods.")]
		public System.Security.AccessControl.GenericSecurityDescriptor SecurityDescriptor
		{
#pragma warning disable 0618
			get
			{
				return GetSecurityDescriptor(Task.defaultSecurityInfosSections);
			}
			set
			{
				SetSecurityDescriptor(value, Task.defaultSecurityInfosSections);
			}
#pragma warning restore 0618
		}

		/// <summary>
		/// Gets all the subfolders in the folder.
		/// </summary>
		public TaskFolderCollection SubFolders
		{
			get
			{
				try
				{
					if (v2Folder != null)
						return new TaskFolderCollection(this, v2Folder.GetFolders(0));
				} catch { }
				return new TaskFolderCollection();
			}
		}

		/// <summary>
		/// Gets a collection of all the tasks in the folder.
		/// </summary>
		public TaskCollection Tasks => GetTasks();

		/// <summary>
		/// Gets or sets the <see cref="TaskService"/> that manages this task.
		/// </summary>
		/// <value>The task service.</value>
		public TaskService TaskService { get; }

		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>
		/// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />.
		/// </returns>
		int IComparable<TaskFolder>.CompareTo(TaskFolder other) => string.Compare(Path, other.Path, true);

		/// <summary>
		/// Creates a folder for related tasks. Not available to Task Scheduler 1.0.
		/// </summary>
		/// <param name="subFolderName">The name used to identify the folder. If "FolderName\SubFolder1\SubFolder2" is specified, the entire folder tree will be created if the folders do not exist. This parameter can be a relative path to the current <see cref="TaskFolder"/> instance. The root task folder is specified with a backslash (\). An example of a task folder path, under the root task folder, is \MyTaskFolder. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
		/// <param name="sd">The security descriptor associated with the folder.</param>
		/// <returns>A <see cref="TaskFolder"/> instance that represents the new subfolder.</returns>
		[Obsolete("This method will be removed in deference to the CreateFolder(string, TaskSecurity) method.")]
		public TaskFolder CreateFolder(string subFolderName, System.Security.AccessControl.GenericSecurityDescriptor sd) => CreateFolder(subFolderName, sd == null ? null : sd.GetSddlForm(Task.defaultAccessControlSections));

		/// <summary>
		/// Creates a folder for related tasks. Not available to Task Scheduler 1.0.
		/// </summary>
		/// <param name="subFolderName">The name used to identify the folder. If "FolderName\SubFolder1\SubFolder2" is specified, the entire folder tree will be created if the folders do not exist. This parameter can be a relative path to the current <see cref="TaskFolder"/> instance. The root task folder is specified with a backslash (\). An example of a task folder path, under the root task folder, is \MyTaskFolder. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
		/// <param name="folderSecurity">The task security associated with the folder.</param>
		/// <returns>A <see cref="TaskFolder"/> instance that represents the new subfolder.</returns>
		public TaskFolder CreateFolder(string subFolderName, TaskSecurity folderSecurity)
		{
			if (folderSecurity == null)
				throw new ArgumentNullException(nameof(folderSecurity));
			return CreateFolder(subFolderName, folderSecurity.GetSecurityDescriptorSddlForm(Task.defaultAccessControlSections));
		}

		/// <summary>
		/// Creates a folder for related tasks. Not available to Task Scheduler 1.0.
		/// </summary>
		/// <param name="subFolderName">The name used to identify the folder. If "FolderName\SubFolder1\SubFolder2" is specified, the entire folder tree will be created if the folders do not exist. This parameter can be a relative path to the current <see cref="TaskFolder" /> instance. The root task folder is specified with a backslash (\). An example of a task folder path, under the root task folder, is \MyTaskFolder. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
		/// <param name="sddlForm">The security descriptor associated with the folder.</param>
		/// <param name="exceptionOnExists">Set this value to false to avoid having an exception called if the folder already exists.</param>
		/// <returns>A <see cref="TaskFolder" /> instance that represents the new subfolder.</returns>
		/// <exception cref="System.Security.SecurityException">Security descriptor mismatch between specified credentials and credentials on existing folder by same name.</exception>
		/// <exception cref="System.ArgumentException">Invalid SDDL form.</exception>
		/// <exception cref="Microsoft.Win32.TaskScheduler.NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		public TaskFolder CreateFolder(string subFolderName, string sddlForm = null, bool exceptionOnExists = true)
		{
			if (v2Folder != null)
			{
				V2Interop.ITaskFolder ifld = null;
				try { ifld = v2Folder.CreateFolder(subFolderName, sddlForm); }
				catch (System.Runtime.InteropServices.COMException ce)
				{
					int serr = ce.ErrorCode & 0x0000FFFF;
					if (serr == 0xb7) // ERROR_ALREADY_EXISTS
					{
						if (exceptionOnExists) throw;
						try
						{
							ifld = v2Folder.GetFolder(subFolderName);
							if (ifld != null && sddlForm != null && sddlForm.Trim().Length > 0)
							{
								string sd = ifld.GetSecurityDescriptor((int)Task.defaultSecurityInfosSections);
								if (string.Compare(sddlForm, sd, true) != 0)
									throw new System.Security.SecurityException("Security descriptor mismatch between specified credentials and credentials on existing folder by same name.");
							}
						}
						catch
						{
							if (ifld != null)
								System.Runtime.InteropServices.Marshal.ReleaseComObject(ifld);
							throw;
						}
					}
					else if (serr == 0x534 || serr == 0x538 || serr == 0x539 || serr == 0x53A || serr == 0x519 || serr == 0x57)
						throw new ArgumentException("Invalid SDDL form", nameof(sddlForm), ce);
					else
						throw;
				}
				return new TaskFolder(TaskService, ifld);
			}
			throw new NotV1SupportedException();
		}

		/// <summary>
		/// Deletes a subfolder from the parent folder. Not available to Task Scheduler 1.0.
		/// </summary>
		/// <param name="subFolderName">The name of the subfolder to be removed. The root task folder is specified with a backslash (\). This parameter can be a relative path to the folder you want to delete. An example of a task folder path, under the root task folder, is \MyTaskFolder. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
		/// <param name="exceptionOnNotExists">Set this value to false to avoid having an exception called if the folder does not exist.</param>
		/// <exception cref="Microsoft.Win32.TaskScheduler.NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		public void DeleteFolder(string subFolderName, bool exceptionOnNotExists = true)
		{
			if (v2Folder != null)
			{
				try
				{
					v2Folder.DeleteFolder(subFolderName, 0);
				}
				catch (Exception e)
				{
					if (!(e is System.IO.FileNotFoundException || e is System.IO.DirectoryNotFoundException) || exceptionOnNotExists)
						throw;
				}
			}
			else
				throw new NotV1SupportedException();
		}

		/// <summary>
		/// Deletes a task from the folder.
		/// </summary>
		/// <param name="Name">The name of the task that is specified when the task was registered. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
		/// <param name="exceptionOnNotExists">Set this value to false to avoid having an exception called if the task does not exist.</param>
		public void DeleteTask(string Name, bool exceptionOnNotExists = true)
		{
			try
			{
				if (v2Folder != null)
					v2Folder.DeleteTask(Name, 0);
				else
				{
					if (!Name.EndsWith(".job", StringComparison.CurrentCultureIgnoreCase))
						Name += ".job";
					v1List.Delete(Name);
				}
			}
			catch (System.IO.FileNotFoundException)
			{
				if (exceptionOnNotExists)
					throw;
			}
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
		/// <returns>
		///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (obj is TaskFolder)
			{
				TaskFolder val = obj as TaskFolder;
				return Path == val.Path && TaskService.TargetServer == val.TaskService.TargetServer && GetSecurityDescriptorSddlForm() == val.GetSecurityDescriptorSddlForm();
			}
			return false;
		}

		/// <summary>
		/// Gets a <see cref="TaskSecurity"/> object that encapsulates the specified type of access control list (ACL) entries for the task described by the current <see cref="TaskFolder"/> object.
		/// </summary>
		/// <returns>A <see cref="TaskSecurity"/> object that encapsulates the access control rules for the current folder.</returns>
		public TaskSecurity GetAccessControl() => GetAccessControl(Task.defaultAccessControlSections);

		/// <summary>
		/// Gets a <see cref="TaskSecurity"/> object that encapsulates the specified type of access control list (ACL) entries for the task folder described by the current <see cref="TaskFolder"/> object.
		/// </summary>
		/// <param name="includeSections">One of the <see cref="System.Security.AccessControl.AccessControlSections"/> values that specifies which group of access control entries to retrieve.</param>
		/// <returns>A <see cref="TaskSecurity"/> object that encapsulates the access control rules for the current folder.</returns>
		public TaskSecurity GetAccessControl(System.Security.AccessControl.AccessControlSections includeSections) => new TaskSecurity(this, includeSections);

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode() => new { A = Path, B = TaskService.TargetServer, C = GetSecurityDescriptorSddlForm() }.GetHashCode();

		/// <summary>
		/// Gets the security descriptor for the folder. Not available to Task Scheduler 1.0.
		/// </summary>
		/// <param name="includeSections">Section(s) of the security descriptor to return.</param>
		/// <returns>The security descriptor for the folder.</returns>
		[Obsolete("This method will be removed in deference to the GetAccessControl and GetSecurityDescriptorSddlForm methods.")]
		public System.Security.AccessControl.GenericSecurityDescriptor GetSecurityDescriptor(System.Security.AccessControl.SecurityInfos includeSections = Task.defaultSecurityInfosSections) => new System.Security.AccessControl.RawSecurityDescriptor(GetSecurityDescriptorSddlForm(includeSections));

		/// <summary>
		/// Gets the security descriptor for the folder. Not available to Task Scheduler 1.0.
		/// </summary>
		/// <param name="includeSections">Section(s) of the security descriptor to return.</param>
		/// <returns>The security descriptor for the folder.</returns>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		public string GetSecurityDescriptorSddlForm(System.Security.AccessControl.SecurityInfos includeSections = Task.defaultSecurityInfosSections)
		{
			if (v2Folder != null)
				return v2Folder.GetSecurityDescriptor((int)includeSections);
			throw new NotV1SupportedException();
		}

		/// <summary>
		/// Gets a collection of all the tasks in the folder whose name matches the optional <paramref name="filter"/>.
		/// </summary>
		/// <param name="filter">The optional name filter expression.</param>
		/// <returns>Collection of all matching tasks.</returns>
		public TaskCollection GetTasks(System.Text.RegularExpressions.Regex filter = null)
		{
			if (v2Folder != null)
				return new TaskCollection(this, v2Folder.GetTasks(1), filter);
			return new TaskCollection(TaskService, filter);
		}

		/// <summary>
		/// Imports a <see cref="Task"/> from an XML file.
		/// </summary>
		/// <param name="Path">The task name. If this value is NULL, the task will be registered in the root task folder and the task name will be a GUID value that is created by the Task Scheduler service. A task name cannot begin or end with a space character. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
		/// <param name="xmlFile">The file containing the XML-formatted definition of the task.</param>
		/// <returns>A <see cref="Task"/> instance that represents the new task.</returns>
		/// <exception cref="NotV1SupportedException">Importing from an XML file is only supported under Task Scheduler 2.0.</exception>
		public Task ImportTask(string Path, string xmlFile) => RegisterTask(Path, System.IO.File.ReadAllText(xmlFile));

		/// <summary>
		/// Registers (creates) a new task in the folder using XML to define the task.
		/// </summary>
		/// <param name="Path">The task name. If this value is NULL, the task will be registered in the root task folder and the task name will be a GUID value that is created by the Task Scheduler service. A task name cannot begin or end with a space character. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
		/// <param name="XmlText">An XML-formatted definition of the task.</param>
		/// <param name="createType">A union of <see cref="TaskCreation"/> flags.</param>
		/// <param name="UserId">The user credentials used to register the task.</param>
		/// <param name="password">The password for the userId used to register the task.</param>
		/// <param name="LogonType">A <see cref="TaskLogonType"/> value that defines what logon technique is used to run the registered task.</param>
		/// <param name="sddl">The security descriptor associated with the registered task. You can specify the access control list (ACL) in the security descriptor for a task in order to allow or deny certain users and groups access to a task.</param>
		/// <returns>A <see cref="Task"/> instance that represents the new task.</returns>
		public Task RegisterTask(string Path, string XmlText, TaskCreation createType = TaskCreation.CreateOrUpdate, string UserId = null, string password = null, TaskLogonType LogonType = TaskLogonType.S4U, string sddl = null)
		{
			if (v2Folder != null)
				return Task.CreateTask(TaskService, v2Folder.RegisterTask(Path, XmlText, (int)createType, UserId, password, LogonType, sddl));

			try
			{
				TaskDefinition td = TaskService.NewTask();
				XmlSerializationHelper.ReadObjectFromXmlText(XmlText, td);
				return RegisterTaskDefinition(Path, td, createType, UserId == null ? td.Principal.ToString() : UserId,
					password, LogonType == TaskLogonType.S4U ? td.Principal.LogonType : LogonType, sddl);
			}
			catch
			{
				throw; // new NotV1SupportedException();
			}
		}

		/// <summary>
		/// Registers (creates) a task in a specified location using a <see cref="TaskDefinition"/> instance to define a task.
		/// </summary>
		/// <param name="Path">The task name. If this value is NULL, the task will be registered in the root task folder and the task name will be a GUID value that is created by the Task Scheduler service. A task name cannot begin or end with a space character. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
		/// <param name="definition">The <see cref="TaskDefinition"/> of the registered task.</param>
		/// <returns>A <see cref="Task"/> instance that represents the new task.</returns>
		public Task RegisterTaskDefinition(string Path, TaskDefinition definition) => RegisterTaskDefinition(Path, definition, TaskCreation.CreateOrUpdate,
				definition.Principal.ToString(), null, definition.Principal.LogonType, null);

		/// <summary>
		/// Registers (creates) a task in a specified location using a <see cref="TaskDefinition" /> instance to define a task.
		/// </summary>
		/// <param name="Path">The task name. If this value is NULL, the task will be registered in the root task folder and the task name will be a GUID value that is created by the Task Scheduler service. A task name cannot begin or end with a space character. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
		/// <param name="definition">The <see cref="TaskDefinition" /> of the registered task.</param>
		/// <param name="createType">A union of <see cref="TaskCreation" /> flags.</param>
		/// <param name="UserId">The user credentials used to register the task.</param>
		/// <param name="password">The password for the userId used to register the task.</param>
		/// <param name="LogonType">A <see cref="TaskLogonType" /> value that defines what logon technique is used to run the registered task.</param>
		/// <param name="sddl">The security descriptor associated with the registered task. You can specify the access control list (ACL) in the security descriptor for a task in order to allow or deny certain users and groups access to a task.</param>
		/// <returns>
		/// A <see cref="Task" /> instance that represents the new task. This will return <c>null</c> if <paramref name="createType"/> is set to <c>ValidateOnly</c> and there are no validation errors.
		/// </returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// Path;Task names may not include any characters which are invalid for file names.
		/// or
		/// Path;Task names ending with a period followed by three or fewer characters cannot be retrieved due to a bug in the native library.
		/// </exception>
		/// <exception cref="NotV1SupportedException">This LogonType is not supported on Task Scheduler 1.0.
		/// or
		/// Security settings are not available on Task Scheduler 1.0.
		/// or
		/// Registration triggers are not available on Task Scheduler 1.0.
		/// or
		/// XML validation not available on Task Scheduler 1.0.</exception>
		public Task RegisterTaskDefinition(string Path, TaskDefinition definition, TaskCreation createType, string UserId, string password = null, TaskLogonType LogonType = TaskLogonType.S4U, string sddl = null)
		{
			if (definition.Actions.Count < 1 || definition.Actions.Count > 32)
			{
				throw new ArgumentOutOfRangeException(nameof(definition.Actions), "A task must be registered with at least one action and no more than 32 actions.");
			}
			UserId = UserId ?? definition.Principal.UserId;
			User user = new User(UserId);
			if (v2Folder != null)
			{
				definition.Actions.ConvertUnsupportedActions();
				if (LogonType == TaskLogonType.ServiceAccount)
				{
					if (string.IsNullOrEmpty(UserId) || !user.IsServiceAccount)
						throw new ArgumentException("A valid system account name must be supplied for TaskLogonType.ServiceAccount. Valid entries are \"NT AUTHORITY\\SYSTEM\", \"SYSTEM\", \"NT AUTHORITY\\LOCALSERVICE\", or \"NT AUTHORITY\\NETWORKSERVICE\".", nameof(UserId));
					if (password != null)
						throw new ArgumentException("A password cannot be supplied when specifying TaskLogonType.ServiceAccount.", nameof(password));
				}
				/*else if ((LogonType == TaskLogonType.Password || LogonType == TaskLogonType.InteractiveTokenOrPassword ||
					(LogonType == TaskLogonType.S4U && UserId != null && !user.IsCurrent)) && password == null)
				{
					throw new ArgumentException("A password must be supplied when specifying TaskLogonType.Password or TaskLogonType.InteractiveTokenOrPassword or TaskLogonType.S4U from another account.", nameof(password));
				}*/
				else if (LogonType == TaskLogonType.Group && password != null)
				{
					throw new ArgumentException("A password cannot be supplied when specifying TaskLogonType.Group.", nameof(password));
				}
				// The following line compensates for an omission in the native library that never actually sets the registration date (thanks ixm7).
				if (definition.RegistrationInfo.Date == DateTime.MinValue) definition.RegistrationInfo.Date = DateTime.Now;
				var iRegTask = v2Folder.RegisterTaskDefinition(Path, definition.v2Def, (int)createType, UserId ?? user.Name, password, LogonType, sddl);
				if (createType == TaskCreation.ValidateOnly && iRegTask == null)
					return null;
				return Task.CreateTask(TaskService, iRegTask);
			}

			// Check for V1 invalid task names
			string invChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
			if (System.Text.RegularExpressions.Regex.IsMatch(Path, @"[" + invChars + @"]"))
				throw new ArgumentOutOfRangeException(nameof(Path), "Task names may not include any characters which are invalid for file names.");
			if (System.Text.RegularExpressions.Regex.IsMatch(Path, @"\.[^" + invChars + @"]{0,3}\z"))
				throw new ArgumentOutOfRangeException(nameof(Path), "Task names ending with a period followed by three or fewer characters cannot be retrieved due to a bug in the native library.");

			// Adds ability to set a password for a V1 task. Provided by Arcao.
			V1Interop.TaskFlags flags = definition.v1Task.GetFlags();
			if (LogonType == TaskLogonType.InteractiveTokenOrPassword && string.IsNullOrEmpty(password))
				LogonType = TaskLogonType.InteractiveToken;
			switch (LogonType)
			{
				case TaskLogonType.Group:
				case TaskLogonType.S4U:
				case TaskLogonType.None:
					throw new NotV1SupportedException("This LogonType is not supported on Task Scheduler 1.0.");
				case TaskLogonType.InteractiveToken:
					flags |= (V1Interop.TaskFlags.RunOnlyIfLoggedOn | V1Interop.TaskFlags.Interactive);
					definition.v1Task.SetAccountInformation(user.Name, IntPtr.Zero);
					break;
				case TaskLogonType.ServiceAccount:
					flags &= ~(V1Interop.TaskFlags.Interactive | V1Interop.TaskFlags.RunOnlyIfLoggedOn);
					definition.v1Task.SetAccountInformation((String.IsNullOrEmpty(UserId) || user.IsSystem) ? String.Empty : user.Name, IntPtr.Zero);
					break;
				case TaskLogonType.InteractiveTokenOrPassword:
					flags |= V1Interop.TaskFlags.Interactive;
					using (V1Interop.CoTaskMemString cpwd = new V1Interop.CoTaskMemString(password))
						definition.v1Task.SetAccountInformation(user.Name, cpwd.DangerousGetHandle());
					break;
				case TaskLogonType.Password:
					using (V1Interop.CoTaskMemString cpwd = new V1Interop.CoTaskMemString(password))
						definition.v1Task.SetAccountInformation(user.Name, cpwd.DangerousGetHandle());
					break;
				default:
					break;
			}
			definition.v1Task.SetFlags(flags);

			switch (createType)
			{
				case TaskCreation.Create:
				case TaskCreation.CreateOrUpdate:
				case TaskCreation.Disable:
				case TaskCreation.Update:
					if (createType == TaskCreation.Disable)
						definition.Settings.Enabled = false;
					definition.V1Save(Path);
					break;
				case TaskCreation.DontAddPrincipalAce:
					throw new NotV1SupportedException("Security settings are not available on Task Scheduler 1.0.");
				case TaskCreation.IgnoreRegistrationTriggers:
					throw new NotV1SupportedException("Registration triggers are not available on Task Scheduler 1.0.");
				case TaskCreation.ValidateOnly:
					throw new NotV1SupportedException("XML validation not available on Task Scheduler 1.0.");
				default:
					break;
			}
			return new Task(TaskService, definition.v1Task);
		}

		/// <summary>
		/// Applies access control list (ACL) entries described by a <see cref="TaskSecurity"/> object to the file described by the current <see cref="TaskFolder"/> object.
		/// </summary>
		/// <param name="taskSecurity">A <see cref="TaskSecurity"/> object that describes an access control list (ACL) entry to apply to the current folder.</param>
		public void SetAccessControl(TaskSecurity taskSecurity)
		{
			taskSecurity.Persist(this);
		}

		/// <summary>
		/// Sets the security descriptor for the folder. Not available to Task Scheduler 1.0.
		/// </summary>
		/// <param name="sd">The security descriptor for the folder.</param>
		/// <param name="includeSections">Section(s) of the security descriptor to set.</param>
		[Obsolete("This method will be removed in deference to the SetAccessControl and SetSecurityDescriptorSddlForm methods.")]
		public void SetSecurityDescriptor(System.Security.AccessControl.GenericSecurityDescriptor sd, System.Security.AccessControl.SecurityInfos includeSections = Task.defaultSecurityInfosSections)
		{
			SetSecurityDescriptorSddlForm(sd.GetSddlForm((System.Security.AccessControl.AccessControlSections)includeSections));
		}

		/// <summary>
		/// Sets the security descriptor for the folder. Not available to Task Scheduler 1.0.
		/// </summary>
		/// <param name="sddlForm">The security descriptor for the folder.</param>
		/// <param name="options">Flags that specify how to set the security descriptor.</param>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		public void SetSecurityDescriptorSddlForm(string sddlForm, TaskSetSecurityOptions options = TaskSetSecurityOptions.None)
		{
			if (v2Folder != null)
				v2Folder.SetSecurityDescriptor(sddlForm, (int)options);
			else
				throw new NotV1SupportedException();
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString() => Path;

		/// <summary>
		/// Enumerates the tasks in the specified folder and its child folders.
		/// </summary>
		/// <param name="folder">The folder in which to start enumeration.</param>
		/// <returns>A <see cref="System.Collections.Generic.IEnumerator{Task}"/> that can be used to iterate through the tasks.</returns>
		private static System.Collections.Generic.IEnumerable<Task> EnumerateFolderTasks(TaskFolder folder)
		{
			foreach (var task in folder.Tasks)
			{
				yield return task;
			}

			foreach (var sfld in folder.SubFolders)
			{
				foreach (var task in EnumerateFolderTasks(sfld))
				{
					yield return task;
				}
			}
		}
	}
}
