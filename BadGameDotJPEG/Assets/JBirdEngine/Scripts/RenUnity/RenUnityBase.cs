﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Linq;

// TODO:
// Add library of story branches
// Add library of characters
// Add button to check for parsing errors
//    Verify Characters (speaking/entrance/exit)
//    Verify Moods
//    Verify Jumps
//    Verify Stats (set/check)
// Add actual decoding of story branches into UI
//    GoToNextLine
//    ChooseOption
// Add "none" keyword for not jumping to a new branch
// Add ability to add multiple commands in story branches
// Add name of character speaking (#[name]:)
// Add character moods/portraits (#mood [name] [mood]:)
// Add character entrances/exits (#enter [name] (location):/#exit [name] (location):) - default to center
// Add character stats changing (#stat [name] [stat] [n]:) - also for option consequence
// Add backgrounds (#bg [image]:)
// Add fade (#fade [seconds]:) - also for option consequences
// Add if statements to options (#if [stat] [n]:) - only for option consequence
// Add if statements to branching (#if [stat] [n] [jumpTo]: #jump [defaultJumpTo]) - also for option consequence, otherwise happens immediately

namespace JBirdEngine {

	namespace RenUnity {

		/// <summary>
		/// RenUnity base class for viewing variables via editor script.
		/// </summary>
		public class RenUnityBase : MonoBehaviour { 
		
			public static RenUnityBase singleton;

			public List<string> conditionalFlags;

			public List<CharacterData> characterData;

			void Awake () {
				if (singleton == null) {
					singleton = this;
				}
				CharacterDatabase.characters = new List<CharacterData>(characterData);
			}

			public void GetCharacters () {
				List<CharacterData> newData = CharacterDatabase.AddAllCharacters();
				for (int i = 0; i < newData.Count; i++) {
					bool charFound = false;
					if (i < characterData.Count && i < System.Enum.GetValues(typeof(Character)).Length) {
						charFound = true;
					}
					if (charFound) {
						for (int j = 0; j < newData[i].stats.Count; j++) {
							bool statFound = false;
							if (j < characterData[i].stats.Count && j < System.Enum.GetValues(typeof(Stat)).Length) {
								statFound = true;
							}
							if (statFound) {
								newData[i].stats[j] = characterData[i].stats[j];
							}
						}
					}
				}
				characterData = newData;
			}
		
		}

		/// <summary>
		/// Branch class. Contains dialogue objects.
		/// </summary>
		[System.Serializable]
		public class Branch {

			public string branchName;
			public List<string> script;

			public Branch () {
				script = new List<string>();
			}

		}

        public static class StoryBranchJsonSerializer {

            public static Branch Read (TextAsset file) {
                Branch newBranch = new Branch();
                using (StringReader reader = new StringReader(file.text)) {
                    string json = reader.ReadToEnd();
                    newBranch = JsonUtility.FromJson<Branch>(json);
                }
                return newBranch;
            }

            public static void Write (string fileName, Branch branch) {
                string json = JsonUtility.ToJson(branch, true);
                using (FileStream writer = new FileStream(fileName, FileMode.OpenOrCreate)) {
                    writer.Write(Encoding.UTF8.GetBytes(json), 0, Encoding.UTF8.GetByteCount(json));
                }
            }

        }

		public static class ConditionalFlags {

			public static List<string> flags = new List<string>();

			public static bool FlagExists (string flagName) {
				for (int i = flags.Count - 1; i >= 0; i--) {
					if (flags[i] == flagName) {
						flags.RemoveAt(i);
						flags.Add(flagName);
						if (RenUnityBase.singleton != null) {
							RenUnityBase.singleton.conditionalFlags.RemoveAt(i);
							RenUnityBase.singleton.conditionalFlags.Add(flagName);
						}
						return true;
					}
				}
				return false;
			}

			public static void AddFlag (string flagName) {
				if (!FlagExists(flagName)) {
					flags.Add(flagName);
					if (RenUnityBase.singleton != null) {
						RenUnityBase.singleton.conditionalFlags.Add(flagName);
					}
				}
			}

		}

		[System.Serializable]
		public class StatData {

			public Stat stat;
			public float value;

			public StatData (Stat s, float v) {
				stat = s;
				value = v;
			}

		}

		[System.Serializable]
		public class CharacterData {

			public Character name;
			public List<StatData> stats;

			public CharacterData (Character c) {
				name = c;
				stats = new List<StatData>();
			}

		}

		public static class CharacterDatabase {

			public static List<CharacterData> characters = AddAllCharacters();

			public static List<CharacterData> AddAllCharacters () {
				List<CharacterData> returnList = new List<CharacterData>();
				for (int i = 1; i < System.Enum.GetValues(typeof(Character)).Length; i++) {
					CharacterData newChar = new CharacterData((Character)i);
					for (int j = 1; j < System.Enum.GetValues(typeof(Stat)).Length; j++) {
						newChar.stats.Add(new StatData((Stat)j, 0f));
					}
					returnList.Add(newChar);
				}
				return returnList;
			}

		}

		public static class DialogueParser {

			public enum CommandType {
				Message,
				StartTalk,
				StopTalk,
				Option,
				Jump,
				SetFlag,
				SetStat,
			}

			public enum ConditionalType {
				Flag,
				Stat,
			}

			public enum EvaluationType {
				Equals,
				NotEquals,
				Less,
				Greater,
				LessEqual,
				GreaterEqual,
			}

			public class ConditionalInfo {

				public ConditionalType type;
				public string flag;
				public Character character;
				public Stat stat;
				public EvaluationType evalType;
				public float value;

				public ConditionalInfo () {
					
				}

				public override string ToString () {
					if (type == ConditionalType.Flag) {
						return string.Format("if flag {0}", flag);
					}
					else if (type == ConditionalType.Stat) {
						string eval = string.Empty;
						switch (evalType) {
						case EvaluationType.Equals:
							eval = string.Concat(eval, "==");
							break;
						case EvaluationType.NotEquals:
							eval = string.Concat(eval, "!=");
							break;
						case EvaluationType.Greater:
							eval = string.Concat(eval, ">");
							break;
						case EvaluationType.GreaterEqual:
							eval = string.Concat(eval, ">=");
							break;
						case EvaluationType.Less:
							eval = string.Concat(eval, "<");
							break;
						case EvaluationType.LessEqual:
							eval = string.Concat(eval, "<=");
							break;
						}
						return string.Format("if stat {0} {1} {2} {3}", character.ToString(), stat.ToString(), eval, value);
					}
					return string.Empty;
				}

				public bool Evaluate () {
					switch (type) {
					case ConditionalType.Flag:
						return ConditionalFlags.FlagExists(flag);
					case ConditionalType.Stat:
						switch (evalType) {
						case EvaluationType.Equals:
							return CharacterDatabase.characters[(int)character].stats[(int)stat].value == value;
						case EvaluationType.NotEquals:
							return CharacterDatabase.characters[(int)character].stats[(int)stat].value == value;
						case EvaluationType.Greater:
							return CharacterDatabase.characters[(int)character].stats[(int)stat].value > value;
						case EvaluationType.GreaterEqual:
							return CharacterDatabase.characters[(int)character].stats[(int)stat].value >= value;
						case EvaluationType.Less:
							return CharacterDatabase.characters[(int)character].stats[(int)stat].value < value;
						case EvaluationType.LessEqual:
							return CharacterDatabase.characters[(int)character].stats[(int)stat].value <= value;
						default:
							return false;
						}
					default:
						return false;
					}
				}

				public int GetLength () {
					if (type == ConditionalType.Flag) {
						return 3;
					}
					if (type == ConditionalType.Stat) {
						return 6;
					}
					return 0;
				}

			}

			public class CommandInfo {

				public CommandType type;
				public string message;
				public Character character;
				public Stat stat;
				public bool relative;
				public bool negate;
				public float value;
				public ConditionalInfo availability;
				public ConditionalInfo conditional;
				public int branch;
				public int conditionalBranch;

				public CommandInfo () {
					
				}

				public override string ToString () {
					return string.Format ("[CommandInfo] Type: '{0}', Message: '{1}', Character: '{2}', Stat: '{3}', Relative: {4}, Negate: {5}, Value: {6} Availability: {7}, Conditional: {8}, Branch: {9}, ConditionalBranch: {10}",
						type, message, character, stat, relative, negate, value, availability, conditional, branch, conditionalBranch);
				}

			}

			/// <summary>
			/// Custom predicate function for determining if a string is the empty string.
			/// </summary>
			/// <returns><c>true</c> if the specified str is the empty string; otherwise, <c>false</c>.</returns>
			/// <param name="str">String to check.</param>
			public static bool IsEmptyString (this string str) {
				return str == string.Empty;
			}

			public static bool IsCommand (this string line) {
				if (line == string.Empty) {
					return false;
				}
				return (line[0] == '/');
			}

			private static Character VerifyCharacter (string name, int lineNumber = -1) {
				Character character = Character.InvalidName;
				if (!EnumHelper.TryParse<Character>(name, out character)) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid name '{0}' (line {1}).", name, lineNumber);
				}
				else if (character == Character.InvalidName) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Did you really name a character 'InvalidName'? Really? (line {0})", lineNumber);
				}
				return character;
			}

			private static Stat VerifyStat (string name, int lineNumber = -1) {
				Stat stat = Stat.InvalidStat;
				if (!EnumHelper.TryParse<Stat>(name, out stat)) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid stat '{0}' (line {1}).", name, lineNumber);
				}
				else if (stat == Stat.InvalidStat) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Did you really name a stat 'InvalidStat'? Really? (line {0})", lineNumber);
				}
				return stat;
			}

			public static List<string> Tokenize (this string line, params char[] splitChars) {
				List<string> returnList = new List<string>();
				List<char> splitCharList = splitChars.ToList();
				returnList.Add(string.Empty);
				for (int i = 0; i < line.Length; i++) {
					if (splitCharList.Contains(line[i])) {
						returnList.Add(string.Empty);
					}
					else {
						returnList[returnList.Count - 1] = string.Concat(returnList[returnList.Count - 1], line[i]);
					}
				}
				returnList.RemoveAll(IsEmptyString);
				return returnList;
			}

			public static CommandInfo ParseLine (string line, int lineNumber = -1) {
				CommandInfo info = new CommandInfo();
				if (!line.IsCommand()) {
					if (line.IsEmptyString()) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Empty line detected (line {0}).", lineNumber);
						return null;
					}
					info.type = CommandType.Message;
					info.message = line;
					return info;
				}
				List<string> tokens = line.Tokenize(' ', '/');
				if (tokens.Count == 0) {
					return null;
				}
				string command = tokens[0];
				switch (command) {
				case "start_talk":
					if (tokens.Count != 2) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid use of '/start_talk' command (line {0}). Correct syntax is '/start_talk [characterName]'.", lineNumber);
						return null;
					}
					return StartTalk(tokens[1], lineNumber);
				case "stop_talk":
					if (tokens.Count != 2) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid use of '/stop_talk' command (line {0}). Correct syntax is '/stop_talk [characterName]'.", lineNumber);
						return null;
					}
					return StopTalk(tokens[1], lineNumber);
				case "option":
					if (tokens.Count < 3) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid use of '/option' command (line {0}). Correct syntax is '/option (availabilityConditional) \"[text]\" (conditional branchIndex) [branchIndex]'.", lineNumber);
						return null;
					}
					return Option(tokens, lineNumber);
				case "jump":
					if (tokens.Count != 2 && tokens.Count != 5 && tokens.Count != 8) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid use of '/jump' command (line {0}). Correct syntax is '/jump (conditional) [branchIndex]'.", lineNumber);
						return null;
					}
					return Jump(tokens, lineNumber);
				case "jump_back":
					if (tokens.Count != 1) {
						Debug.LogWarningFormat("RenUnity.DialogueParser: '/jump_back' command does not require additional arguments (line {0}).", lineNumber);
					}
					info.type = CommandType.Jump;
					info.branch = -1;
					return info;
				case "set_flag":
					if (tokens.Count != 2) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid use of '/set_flag' command (line {0}). Correct syntax is '/set_flag [flagName]'.", lineNumber);
						return null;
					}
					info.type = CommandType.SetFlag;
					info.message = tokens[1];
					return info;
				case "set_stat":
					if (tokens.Count != 4 && tokens.Count != 5) {
						for (int i = 0; i < tokens.Count; i++) {
							Debug.Log(tokens[i]);
						}
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid use of '/set_stat' command (line {0}). Correct syntax is '/set_stat [characterName] [stat] (+,-) [value]'.", lineNumber);
						return null;
					}
					return SetStat(tokens, lineNumber);
				default:
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid command '{0}' (line {1}).{2}Attempted to parse line: '{3}'.", command, lineNumber, System.Environment.NewLine, line);
					return null;
				}
				Debug.LogErrorFormat("RenUnity.DialogueParser: Command '{0}' recognized, but not implemented (line {1}).", tokens[0], lineNumber);
				return null;
			}

			private static ConditionalInfo HandleConditional (List<string> tokens, int startIndex, int lineNumber = -1) {
				ConditionalInfo conditional = new ConditionalInfo();
				if (tokens.Count < startIndex + 1) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid conditional syntax (line {0}). Proper syntax is 'if flag [flagName]' or 'if stat [characterName] [statName] [==, !=, >, >=, <, <=] [value]'", lineNumber);
					return null;
				}
				if (tokens[startIndex] == "flag") {
					conditional.type = ConditionalType.Flag;
					startIndex++;
					if (tokens.Count < startIndex + 1) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid conditional syntax (line {0}). Proper syntax is 'if flag [flagName]'", lineNumber);
						return null;
					}
					conditional.flag = tokens[startIndex];
					return conditional;
				}
				else if (tokens[startIndex] == "stat") {
					conditional.type = ConditionalType.Stat;
					startIndex++;
					if (tokens.Count < startIndex + 1) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid conditional syntax (line {0}). Proper syntax is 'if stat [characterName] [statName] [==, !=, >, >=, <, <=] [value]'", lineNumber);
						return null;
					}
					conditional.character = VerifyCharacter(tokens[startIndex], lineNumber);
					if (conditional.character == Character.InvalidName) {
						return null;
					}
					startIndex++;
					if (tokens.Count < startIndex + 1) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid conditional syntax (line {0}). Proper syntax is 'if stat [characterName] [statName] [==, !=, >, >=, <, <=] [value]'", lineNumber);
						return null;
					}
					conditional.stat = VerifyStat(tokens[startIndex], lineNumber);
					if (conditional.stat == Stat.InvalidStat) {
						return null;
					}
					startIndex++;
					if (tokens.Count < startIndex + 1) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid conditional syntax (line {0}). Proper syntax is 'if stat [characterName] [statName] [==, !=, >, >=, <, <=] [value]'", lineNumber);
						return null;
					}
					switch (tokens[startIndex]) {
					case "==":
						conditional.evalType = EvaluationType.Equals;
						break;
					case "!=":
						conditional.evalType = EvaluationType.NotEquals;
						break;
					case ">":
						conditional.evalType = EvaluationType.Greater;
						break;
					case ">=":
						conditional.evalType = EvaluationType.GreaterEqual;
						break;
					case "<":
						conditional.evalType = EvaluationType.Less;
						break;
					case "<=":
						conditional.evalType = EvaluationType.LessEqual;
						break;
					default:
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid operator '{0}' (line {1}). Accepted operators are: ==, !=, >, >=, <, or <=.", tokens[startIndex], lineNumber);
						return null;
					}
					startIndex++;
					if (tokens.Count < startIndex + 1) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid conditional syntax (line {0}). Proper syntax is 'if stat [characterName] [statName] [==, !=, >, >=, <, <=] [value]'", lineNumber);
						return null;
					}
					float value;
					if (!float.TryParse(tokens[startIndex], out value)) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Non-numerical value '{0}' (line {1}).", tokens[startIndex], lineNumber);
						return null;
					}
					conditional.value = value;
					return conditional;
				}
				else {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid conditional type '{0}' (line {1})", tokens[startIndex], lineNumber);
					return null;
				}
			}

			private static CommandInfo StartTalk (string characterName, int lineNumber = -1) {
				CommandInfo info = new CommandInfo();
				info.type = CommandType.StartTalk;
				Character character = VerifyCharacter(characterName, lineNumber);
				if (character != Character.InvalidName) {
					info.character = character;
					return info;
				}
				return null;
			}

			private static CommandInfo StopTalk (string characterName, int lineNumber = -1) {
				CommandInfo info = new CommandInfo();
				info.type = CommandType.StopTalk;
				Character character = VerifyCharacter(characterName, lineNumber);
				if (character != Character.InvalidName) {
					info.character = character;
					return info;
				}
				return null;
			}

			private static CommandInfo Option (List<string> tokens, int lineNumber = -1) {
				CommandInfo info = new CommandInfo();
				info.type = CommandType.Option;
				int parseIndex = 1;
				if (tokens[parseIndex] == "if") {
					info.availability = HandleConditional(tokens, parseIndex + 1, lineNumber);
					if (info.availability == null) {
						return null;
					}
					parseIndex += info.availability.GetLength();
				}
				if (parseIndex > tokens.Count - 1) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid arguments for '/option' command (line {0}). Proper syntax is '/option (availabilityConditional) \"[text]\" (conditional branchIndex) [branchIndex]'", lineNumber);
					return null;
				}
				if (tokens[parseIndex][0] != '"') {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid arguments for '/option' command (line {0}). Are you perhaps missing quotation marks? Proper syntax is '/option (availabilityConditional) \"[text]\" (conditional branchIndex) [branchIndex]'", lineNumber);
					return null;
				}
				string message = string.Empty;
				bool finishedWriting = false;
				while (parseIndex < tokens.Count) {
					message = string.Concat(message, tokens[parseIndex]);
					if (tokens[parseIndex][tokens[parseIndex].Length - 1] == '"') {
						finishedWriting = true;
						info.message = message;
						parseIndex++;
						break;
					}
					message = string.Concat(message, " ");
					parseIndex++;
				}
				if (!finishedWriting) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid arguments for '/option' command (line {0}). Could not find endquote.", lineNumber);
					return null;
				}
				if (parseIndex > tokens.Count - 1) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid arguments for '/option' command (line {0}). Proper syntax is '/option (availabilityConditional) \"[text]\" (conditional branchIndex) [branchIndex]'", lineNumber);
					return null;
				}
				if (tokens[parseIndex] == "if") {
					info.conditional = HandleConditional(tokens, parseIndex + 1, lineNumber);
					if (info.conditional == null) {
						return null;
					}
					parseIndex += info.conditional.GetLength();
					if (parseIndex > tokens.Count - 1) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid arguments for '/option' command (line {0}). Proper syntax is '/option (availabilityConditional) \"[text]\" (conditional branchIndex) [branchIndex]'", lineNumber);
						return null;
					}
					int cBranch;
					if (!int.TryParse(tokens[parseIndex], out cBranch)) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid branch index '{0}' (line {1}).", tokens[parseIndex], lineNumber);
						return null;
					}
					info.conditionalBranch = cBranch;
					parseIndex++;
				}
				if (parseIndex > tokens.Count - 1) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid arguments for '/option' command (line {0}). Proper syntax is '/option (availabilityConditional) \"[text]\" (conditional branchIndex) [branchIndex]'", lineNumber);
					return null;
				}
				int branch;
				if (!int.TryParse(tokens[parseIndex], out branch)) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid branch index '{0}' (line {1}).", tokens[parseIndex], lineNumber);
					return null;
				}
				info.branch = branch;
				return info;
			}

			private static CommandInfo Jump (List<string> tokens, int lineNumber = -1) {
				CommandInfo info = new CommandInfo();
				info.type = CommandType.Jump;
				int branch;
				if (!int.TryParse(tokens[1], out branch)) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid branch index '{0}' (line {1}).", tokens[1], lineNumber);
					return null;
				}
				info.branch = branch;
				if (tokens[2] == "if") {
					info.conditional = HandleConditional(tokens, 3, lineNumber);
					if (info.conditional == null) {
						return null;
					}
				}
				return info;
			}

			private static CommandInfo SetStat (List<string> tokens, int lineNumber = -1) {
				CommandInfo info = new CommandInfo();
				info.type = CommandType.SetStat;
				//verify character
				Character character = VerifyCharacter(tokens[1], lineNumber);
				if (character == Character.InvalidName) {
					return null;
				}
				info.character = character;
				//verify stat
				Stat stat = VerifyStat(tokens[2], lineNumber);
				if (stat == Stat.InvalidStat) {
					return null;
				}
				info.stat = stat;
				//determine if relative
				bool relative = false;
				bool negate = false;
				int parseIndex = 3;
				if (tokens[3] == "+") {
					relative = true;
					parseIndex = 4;
				}
				else if (tokens[3] == "-") {
					relative = true;
					negate = true;
					parseIndex = 4;
				}
				if (parseIndex == 4) {
					if (tokens.Count != 5) {
						Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid arguments for 'set_stat' command (line {0}).", lineNumber);
						return null;
					}
				}
				info.relative = relative;
				info.negate = negate;
				//verify value
				float value = 0f;
				if (!float.TryParse(tokens[parseIndex], out value)) {
					Debug.LogErrorFormat("RenUnity.DialogueParser: Invalid value '{0}' for 'set_stat' command (line {1}).", tokens[parseIndex], lineNumber);
					return null;
				}
				info.value = value;
				return info;
			}

		}

	}

}
