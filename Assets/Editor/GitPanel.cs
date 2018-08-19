#pragma warning disable 0414
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;

public delegate void Invoke();


public class StringPointer
{
	private string str = null;
	public StringPointer(string str)
	{
		this.str = str;
	}

	public override string ToString()
	{
		return str;
	}
}

class GitPanel : EditorWindow
{

	class ExecuteResponse
	{
		public string message;
		public int exitCode;
		public override string ToString()
		{
			return message;
		}
	}

	private int maxCommits = 10;

	private List<StringPointer> business = new List<StringPointer>();

	private Vector2 scrollPosition = Vector2.zero;

	private Invoke mainThreadInvokeQueue;

	private bool enableDebug = false;
	private bool debugPanel = false;
	private bool busnessDisplay = false;

	private bool advancedOptions = false;

	private string commitMessage = "";

	private string[] changes = { };

	// comment to make msdfgsasdfasdfdfsdfggnd woo
	struct Commit
	{
		public string hash, author, date, subject;
		public bool local;
	}

	private bool changesFoldedOut = true;

	private bool historyFoldedOut = true;

	private Commit[] history = null;

	private string version = "Acquiring version...";

	private GUIStyle headerStyle;
	private GUIStyle subHeaderStyle;
	private GUIStyle monospaced;

	private Color localCommitColor;

	private Color defaultColor;

	private Color defaultBackgroundColor;

	private Color modifiedColor = Color.yellow;
	private Color addedColor = Color.green;
	private Color deletedColor = Color.red;

	//this is a thing to commit woo

	private string _name = "";

	private string currentBranch = null;

	private bool creatingBranch = false;

	private string newBranchName = "";

	private string[] branches = { "a", "b" };

	[MenuItem("Window/Github")]
	public static void ShowWindow()
	{
		EditorWindow.GetWindow(typeof(GitPanel));
	}

	private void log(object o)
	{
		//		UnityEngine.Debug.Log (o.ToString ());
	}

	public void OnEnable()
	{

	}

	public void CreateStyles()
	{
		UnityEngine.Debug.Log("is this happening????");
		headerStyle = new GUIStyle(EditorStyles.label);
		headerStyle.fontSize = 24;
		subHeaderStyle = new GUIStyle(EditorStyles.label);
		subHeaderStyle.fontSize = 12;
		monospaced = new GUIStyle(EditorStyles.label);
		monospaced.font = Resources.Load<Font>("Fonts/UbuntuMono-R.ttf");
		localCommitColor = Color.grey;
		defaultBackgroundColor = Color.white;
		defaultColor = Color.white;
	}

	private void dialogue(string title, string message)
	{
		mainThreadInvokeQueue += new Invoke(() =>
		{
			EditorUtility.DisplayDialog(title, message, "Okay");
		});
	}

	private void dialogue(string message)
	{
		dialogue("Git Panel", message);
	}

	public void Update()
	{
		// this event queue is made to run things that can only be run on the main thread.
		// it was hard, and thus is awesome.
		// basically it just makes popups a thing, cause
		// retarded unity threadlocked them, wtf???
		if (mainThreadInvokeQueue != null)
		{
			mainThreadInvokeQueue();
			mainThreadInvokeQueue = null;
		}
	}
	private void refreshInformation()
	{
		//lambda functions WOO!
		new Thread(() =>
		{
			StringPointer busyLock = new StringPointer("refreshInformation");
			business.Add(busyLock);
			version = (execute("git", "version")).Trim();

			string name = System.IO.Directory.GetCurrentDirectory();
			name = name.Substring(name.LastIndexOf("" + System.IO.Path.DirectorySeparatorChar) + 1);
			name = Regex.Replace(name, "-", m => " ");
			name = name.Trim();
			this._name = name;

			execute("git", "fetch"); // and pray
					string branchesCommand = execute("git", "branch -a");
			string[] branches = branchesCommand.Split('\n');
			for (int i = 0; i < branches.Length; i++)
			{
				branches[i] = branches[i].Trim();
				if (branches[i].StartsWith("*"))
				{
					branches[i] = branches[i].Substring(1).Trim();
					this.currentBranch = branches[i];
				}
				branches[i] = branches[i].Trim();
				if (branches[i].Contains("/"))
				{
					string b = branches[i];
					string r = b.Substring(8);
					r = r.Substring(0, r.IndexOf('/'));
					b = b.Substring(b.LastIndexOf('/') + 1);
							//dialogue(r);
							branches[i] = b;
							//dialogue(branches[i]);
						}
			}
			this.branches = branches;

			mainThreadInvokeQueue += new Invoke(() =>
					{
					business.Remove(busyLock);
					Repaint();
				});
		}).Start();

	}

	private void refreshHistory()
	{
		new Thread(() =>
		{
			StringPointer busyLock = new StringPointer("refreshHistory");
			business.Add(busyLock);
			int commitCount = Math.Min(Int32.Parse(execute("git", "rev-list --count HEAD").Trim()), maxCommits);
			string[] syncedCommits = execute("git", "log @{u} -n " + commitCount + " --pretty=format:\"%h;%an;%ar;%s\"").Trim().Split('\n');
			string[] localCommits = execute("git", "log @{u}..HEAD -n " + commitCount + " --pretty=format:\"%h;%an;%ar;%s\"").Trim().Split('\n');

			history = new Commit[commitCount];

			if (localCommits.Length == 1 && localCommits[0] == "")
				localCommits = new string[] { };

			for (int i = 0; i < localCommits.Length; i++)
			{
				Commit commit;
						//				log(localCommits[i]);
						string[] parts = localCommits[i].Split(';');
				commit.local = true;
				commit.hash = parts[0];
				commit.author = parts[1];
				commit.date = parts[2];
				commit.subject = parts[3];
				history[i] = commit;
			}
			for (int i = 0; i < syncedCommits.Length; i++)
			{
				Commit commit;
						//				log(syncedCommits[i]);
						string[] parts = syncedCommits[i].Split(';');
				commit.local = false;
				commit.hash = parts[0];
				commit.author = parts[1];
				commit.date = parts[2];
				commit.subject = parts[3];
				history[i + localCommits.Length] = commit;
			}
			mainThreadInvokeQueue += new Invoke(() =>
					{
					business.Remove(busyLock);
					Repaint();
				});
		}).Start();
	}

	private void checkout(string branch)
	{
		dialogue(branch);
		new Thread(() =>
		{
			StringPointer busyLock = new StringPointer("checkout");
			business.Add(busyLock);
			ExecuteResponse checkoutResponse = executeWithCode("git", "checkout " + branch);
			if (checkoutResponse.exitCode == 0)
			{
				refreshInformation();
			}
			else
			{
				dialogue("Could not checkout", checkoutResponse.message);

			}
			mainThreadInvokeQueue += new Invoke(() =>
					{
					business.Remove(busyLock);
					Repaint();
				});
		}).Start();
	}

	private string execute(string command, string args)
	{
		ExecuteResponse executeResponse = executeWithCode(command, args);
		return executeResponse.message;
	}

	private ExecuteResponse executeWithCode(string command, string args)
	{
		Process p = new Process();
		p.StartInfo.CreateNoWindow = true;
		p.StartInfo.UseShellExecute = false;
		p.StartInfo.RedirectStandardOutput = true;
		p.StartInfo.RedirectStandardError = true;
		p.StartInfo.FileName = command;
		p.StartInfo.Arguments = args;
		p.Start();

		// To avoid deadlocks, always read the output stream first and then wait.
		string output = "";
		p.WaitForExit();


		ExecuteResponse executeResponse = new ExecuteResponse();
		if (executeResponse.exitCode == 0)
		{
			output = p.StandardOutput.ReadToEnd();
		}
		else
		{
			output = p.StandardError.ReadToEnd();
		}
		executeResponse.exitCode = p.ExitCode;
		executeResponse.message = output;

		return executeResponse;
	}

	public void OnFocus()
	{
		refreshChanges();
		refreshInformation();
		refreshHistory();
	}

	public void refreshChanges()
	{
		new Thread(() =>
		{
			ExecuteResponse gitAdd = executeWithCode("git", "add -A");
			if (gitAdd.exitCode != 0)
			{
				dialogue("git add -A returned exit code " + gitAdd.exitCode, gitAdd.message);
				return;
			}
			int changeLengthBefore = changes.Length;
			string status = execute("git", "-c color.status=false status -s");
			status = status.Trim();
			changes = status.Split('\n');
			if (changes.Length == 1 && changes[0] == "")
				changes = new string[] { };

			if (changeLengthBefore == 0 && changes.Length != 0)
			{
				changesFoldedOut = true;
			}
		}).Start();
		/*
' ' = unmodified

	M = modified

	A = added

	D = deleted

	R = renamed

	C = copied

	U = updated but unmerged*/
	}

	private void CreateCommit()
	{
		new Thread(() =>
		{

			ExecuteResponse gitAdd = executeWithCode("git", "add -A");
			if (gitAdd.exitCode != 0)
			{
				dialogue("git add -A returned exit code " + gitAdd.exitCode, gitAdd.message);
				return;
			}
					//heres a change
					string commitMessage = this.commitMessage == "" ? "No Commit Message" : this.commitMessage.Replace("\"", "'");
			log(commitMessage);

			ExecuteResponse gitCommit = executeWithCode("git", "commit -m \"" + commitMessage + "\"");
			if (gitCommit.exitCode != 0)
			{
				if (!gitCommit.message.Contains("nothing to commit, working tree clean"))
				{
					dialogue("git commit returned exit code " + gitCommit.exitCode, gitCommit.message);
					return;
				}
			}

			this.commitMessage = "";
			refreshChanges();
			refreshHistory();
			changesFoldedOut = false;

		}).Start();
	}

	public void OnGUI()
	{
		//dialogue("" + anyToPush);

		if (headerStyle == null)
			CreateStyles();

		bool haveChanges = changes.Length > 0;
		//        bool anyToPush = execute("git", "log @{u}.. -n 1 --pretty=format:\"%h;%an;%ar;%s\"").Trim() != "";

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
		GUI.backgroundColor = defaultBackgroundColor;

		GUILayout.Space(8);
		GUILayout.Label(_name, headerStyle);
		GUILayout.Label(version, EditorStyles.miniLabel);
		GUILayout.Space(16);
		int branchID = Array.IndexOf(branches, currentBranch);

		if (currentBranch == null)
		{
			GUILayout.Label("Initializing. No branch selected.");
		}
		else
		{
			int newBranchID = EditorGUILayout.Popup("Branch", branchID, branches);
			if (newBranchID != branchID)
			{
				checkout(branches[newBranchID]);
			}
			commitMessage = GUILayout.TextArea(commitMessage, GUILayout.Height(70.0f));
			if (haveChanges)
			{
				if (GUILayout.Button("Commit"))
				{
					CreateCommit();
				}

			}
			else
			{
				GUILayout.BeginHorizontal();

				if (GUILayout.Button("Push"))
				{
					push();
				}
				if (GUILayout.Button("Pull"))
				{
					pull();
				}

				GUILayout.EndHorizontal();
			}

		} // now fold out

		if (changesFoldedOut = EditorGUILayout.Foldout(changesFoldedOut, "View Changes"))
		{
			GUILayout.BeginVertical("box");
			if (haveChanges)
			{
				foreach (string change in changes)
				{
					string str = change.Trim();
					if (str.StartsWith("M"))
						GUI.color = modifiedColor;
					if (str.StartsWith("A"))
						GUI.color = addedColor;
					if (str.StartsWith("D"))
						GUI.color = deletedColor;
					GUILayout.Label(change.Trim(), EditorStyles.miniLabel);
					GUI.color = defaultColor;
				}
			}
			else
			{
				GUILayout.Label("You are up to date!", EditorStyles.miniLabel);
			}
			GUILayout.EndVertical();
		}

		#region commit history
		if (historyFoldedOut = EditorGUILayout.Foldout(historyFoldedOut, "Commit History"))
		{
			//			EditorGUILayout.BeginVertical ("box");
			if (history == null)
			{
				EditorGUILayout.BeginVertical("box");
				GUILayout.Label("Loading commits...");
				EditorGUILayout.EndVertical();
			}
			else
			{
				maxCommits = EditorGUILayout.IntField("History Length", maxCommits);
				//				int hashWidth = 60;
				//				float subjectWidth = (EditorGUIUtility.fieldWidth / 2) - hashWidth - 24;
				//				log (EditorGUIUtility.currentViewWidth);
				//				subjectWidth = 100;
				foreach (Commit commit in history)
				{
					if (commit.local)
					{
						GUI.backgroundColor = Color.Lerp(Color.grey, defaultBackgroundColor, 0.5f);
					}
					else
					{
						GUI.backgroundColor = defaultBackgroundColor;
					}
					EditorGUILayout.BeginVertical("box");
					//					EditorGUILayout.BeginHorizontal ();
					//					GUILayout.Label (commit.subject, EditorStyles.largeLabel, GUILayout.Width(subjectWidth));
					//					GUILayout.Label (commit.hash, monospaced, GUILayout.Width(hashWidth));
					GUILayout.Label(commit.subject, EditorStyles.largeLabel, GUILayout.Width(position.width - 20));
					//					EditorGUILayout.EndHorizontal ();
					GUILayout.Label(commit.date + " by " + commit.author, EditorStyles.miniLabel);
					EditorGUILayout.EndVertical();
				}
				GUI.backgroundColor = defaultBackgroundColor;
			}
			//			EditorGUILayout.EndVertical ();
		}
		#endregion

		if (advancedOptions = EditorGUILayout.Foldout(advancedOptions, "Advanced/Expirimental Options"))
		{
			EditorGUI.indentLevel = 1;
			if (creatingBranch = EditorGUILayout.Foldout(creatingBranch, "Create New Branch"))
			{
				newBranchName = EditorGUILayout.TextField("Branch Name", newBranchName);
				GUILayout.Button("Create New Branch");
				EditorGUI.indentLevel = 0;
			}
			if (enableDebug = GUILayout.Toggle(enableDebug, "Enable Debug Panels"))
			{
				if (debugPanel = EditorGUILayout.Foldout(debugPanel, "Debug Event Triggers"))
				{
					GUILayout.BeginVertical("box");
					if (GUILayout.Button("refreshChanges()"))
						refreshChanges();
					if (GUILayout.Button("RefreshInformation()"))
						refreshInformation();
					if (GUILayout.Button("CreateStyles()"))
						CreateStyles();
					if (GUILayout.Button("push()"))
						push();
					if (GUILayout.Button("resetBusiness()"))
						business.Clear();
					GUILayout.EndVertical();

				}

				if (busnessDisplay = EditorGUILayout.Foldout(busnessDisplay, "Debug Business Array"))
				{
					GUILayout.BeginVertical("box");
					foreach (StringPointer strp in business)
					{
						GUILayout.Label(strp.ToString());
					}
					GUILayout.EndVertical();
				}
			}
		}







		EditorGUILayout.EndScrollView();

	}

	public void push()
	{
		ExecuteResponse gitPush = executeWithCode("git", "push origin " + currentBranch + "");
		if (gitPush.exitCode != 0)
		{
			dialogue("git push returned exit code " + gitPush.exitCode, gitPush.message + "\n\nThis could be cause by invalid credentials\ntry from the command line");
			return;
		}
	}

	public void pull()
	{
		ExecuteResponse gitPull = executeWithCode("git", "pull origin " + currentBranch + "");
		if (gitPull.exitCode != 0)
		{
			dialogue("git push returned exit code " + gitPull.exitCode, gitPull.message + "\n\nThis likely means there are conflicts between you and remote, since your last pull");
			return;
		}
	}
} //boop bop