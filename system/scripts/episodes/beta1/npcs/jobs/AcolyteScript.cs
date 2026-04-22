//--- Sabine Script Conversion ------------------------------------------
// Automatically converted by SabineConverter utility.
//---------------------------------------------------------------------------
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Shared.Database;
using Sabine.Zone.Scripting;
using Sabine.Zone.Scripting.Dialogues;
using Sabine.Zone.World.Actors;
using static Sabine.Zone.Scripting.Shortcuts;

public class AcolyteScript : GeneralScript
{

	private const string VAR_ADVJOB = "quest_ADVJOB";
	private const string VAR_JOB_ACOLYTE = "quest_Job_Acolyte";
	private const string VAR_JOB_CHAMPION = "quest_Job_Champion";
	private const string VAR_JOB_HIGH_PRIEST = "quest_Job_High_Priest";
	private const string VAR_JOB_NOVICE = "quest_Job_Novice";
	private const string VAR_JOB_NOVICE_HIGH = "quest_Job_Novice_High";
	private const string VAR_JOB_PRIEST = "quest_Job_Priest";
	private const string VAR_JOB_ACOLYTE_Q = "quest_job_acolyte_q";

	public override void Load()
	{
		if (!MapsExist("moc_fild07", "prt_church", "prt_fild00", "prt_fild03")) return;

		AddNpc("Cleric", 60, "prt_church", 184, 41, 4, Cleric_60Dialog);
		AddNpc("Ascetic", 89, "prt_fild03", 365, 255, 2, Ascetic_89Dialog);
		AddNpc("Ascetic", 95, "moc_fild07", 41, 355, 4, Ascetic_95Dialog);
		AddNpc("Ascetic", 98, "prt_fild00", 208, 218, 6, Ascetic_98Dialog);
	}

	private async Task Cleric_60Dialog(Dialog dialog)
	{
		var player = dialog.Player;
		{
			if (player.Parameters.Get(ParameterType.Upper) == 1)
			{
				if (player.Vars.Perm.GetInt(VAR_ADVJOB, 0) == player.Vars.Perm.GetInt(VAR_JOB_HIGH_PRIEST, 0) || player.Vars.Perm.GetInt(VAR_ADVJOB, 0) == player.Vars.Perm.GetInt(VAR_JOB_CHAMPION, 0))
				{
					if (player.Parameters.Get(ParameterType.Class) == player.Vars.Perm.GetInt(VAR_JOB_NOVICE_HIGH, 0))
					{
						await dialog.Talk("[Father Mareusis]");
						await dialog.Talk("Ah, I sense you have endured");
						await dialog.Talk("a past life experience. You must have learned many things before entering Valhalla.");
						await dialog.Next();
						if (player.CanChangeJob())
						{
							await dialog.Talk("[Father Mareusis]");
							await dialog.Talk("Unfortunately, I don't think you're ready to become an Acolyte yet. Please finish learning all of the Basic Skills first.");
							await dialog.Next();
							await dialog.Talk("[Father Mareusis]");
							await dialog.Talk("In the meantime,");
							await dialog.Talk("I will wait until");
							await dialog.Talk("you are ready.");
							await dialog.Talk("May God be");
							await dialog.Talk("with you.");
							dialog.Close();
							return;
						}
						await dialog.Talk("[Father Mareusis]");
						await dialog.Talk("Well, I welcome you");
						await dialog.Talk("back from Valhalla and");
						await dialog.Talk("wish you luck on your");
						await dialog.Talk("new life's journey.");
						await dialog.Next();
						// UNHANDLED: skill "NV_TRICKDEAD",0,SKILL_PERM;
						// UNHANDLED: jobchange Job_Acolyte_High; // Needs explicit C# implementation
						// UNHANDLED: skill "AL_HOLYLIGHT",1,SKILL_PERM;
						await dialog.Talk("[Father Mareusis]");
						await dialog.Talk("Now, venture forth and seek those who need your help. May God light your path.");
						dialog.Close();
						return;
					}
					else
					{
						await dialog.Talk("[Father Mareusis]");
						await dialog.Talk("Now, venture forth to seek people who need your help. May God enlighten your way.");
						dialog.Close();
						return;
					}
				}
				else
				{
					await dialog.Talk("[Father Mareusis]");
					await dialog.Talk("I sense that you have endured a past life experience. You must have learned many things before entering Valhalla.");
					await dialog.Next();
					await dialog.Talk("[Father Mareusis]");
					await dialog.Talk("However, I can tell that you are not suited to be an Acolyte. Please remember who you were in your past life and find your path.");
					dialog.Close();
					return;
				}
			}
			await dialog.Talk("[Father Mareusis]");
			await dialog.Talk("What is it that you seek?");
			await dialog.Next();
			switch (await dialog.Select("Father, I want to be a Acolyte.", "Acolyte Requirements.", "Just looking around."))
			{
				case 1:
					await dialog.Talk("[Father Mareusis]");
					if (player.Parameters.Get(ParameterType.BaseJob) == player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE, 0))
					{
						await dialog.Talk("Are you feeling okay today? I can tell by your attire that you are already an Acolyte. You're not joking around, are you?");
						dialog.Close();
						return;
					}
					else if (player.Parameters.Get(ParameterType.BaseJob) != player.Vars.Perm.GetInt(VAR_JOB_NOVICE, 0))
					{
						await dialog.Talk("I'm sorry, but we can only accept Novices as applicants for the job change to Acolyte.");
						dialog.Close();
						return;
					}
					if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) == 0)
					{
						await dialog.Talk("Do you truly");
						await dialog.Talk("wish to become");
						await dialog.Talk("a servant of God?");
						await dialog.Next();
						if (await dialog.Select("Yes Father, I do.", "Nope, I lied.") == 1)
						{
							await dialog.Talk("[Father Mareusis]");
							await dialog.Talk($"Good. I accept {player.Name}'s will to become an Acolyte. You understand that you must do penance before you can become a servant of God, right?");
							await dialog.Next();
							await dialog.Talk("[Father Mareusis]");
							await dialog.Talk("Well, I will");
							await dialog.Talk("give you a mission...");
							switch (ItemScript.Random(0, 2))
							{
								default:
									player.Vars.Perm.Set(VAR_JOB_ACOLYTE_Q, 2);
									await dialog.Talk("Please visit ^000077Father Rubalkabara^000000, a member of the Prontera Parish, and return here. He has been practicing asceticism in the ^000077Relics NorthEast of Prontera City^000000.");
									//player.Quests.Add(1001);
									break;
								case 1:
									player.Vars.Perm.Set(VAR_JOB_ACOLYTE_Q, 3);
									await dialog.Talk("Please visit ^000077Mother Mathilda^000000 and then return to me. She has been practicing asceticism near ^000077Morocc Town, SouthWest of Prontera City^000000.");
									//player.Quests.Add(1002);
									break;
								case 2:
									player.Vars.Perm.Set(VAR_JOB_ACOLYTE_Q, 4);
									await dialog.Talk("Please visit ^000077Father Yosuke^000000 and return here. He has been practicing asceticism around ^000077a bridge somewhere NorthWest of Prontera^000000.");
									//player.Quests.Add(1003);
									break;
							}
							await dialog.Next();
							await dialog.Talk("[Father Mareusis]");
							await dialog.Talk("May the grace of God light your path and guide you during your journey of penance.");
							dialog.Close();
							return;
						}
						await dialog.Talk("[Father Mareusis]");
						await dialog.Talk("You lied?");
						await dialog.Talk("It is good that you");
						await dialog.Talk("have confessed your");
						await dialog.Talk("wrongdoing. Go in");
						await dialog.Talk("peace, my son.");
						dialog.Close();
						return;
					}
					await dialog.Talk("Oh, you've come back. Let me check and see if you are ready to serve God. Let's see...");
					await dialog.Next();
					await dialog.Talk("[Father Mareusis]");
					//if (CanChangeJob(player))
					{
						await dialog.Talk("Good Lord! Haven't you accomplished the Basic Training yet?! It's important that you finish that!");
						await dialog.Next();
						await dialog.Talk("[Father Mareusis]");
						await dialog.Talk("You should have trained more! Go back and make sure you reach Novice Job Level 9 and learn all of the Basic Skills!");
						dialog.Close();
						return;
					}
					if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) < 5)
					{
						await dialog.Talk("Oh? I can't find your name on the Registration List.");
						await dialog.Next();
						switch (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0))
						{
							case 2:
								await dialog.Talk("[Father Mareusis]");
								await dialog.Talk("Please visit ^000077Father Rubalkabara^000000, a member of the Prontera Parish, and return here.");
								await dialog.Next();
								await dialog.Talk("[Father Mareusis]");
								await dialog.Talk("He has been practicing asceticism in the ^000077Relics at the NorthEast of Prontera City^000000.");
								break;
							case 3:
								await dialog.Talk("[Father Mareusis]");
								await dialog.Talk("Please Visit ^000077Mother Mathilda^000000 and return here to me.");
								await dialog.Next();
								await dialog.Talk("[Father Mareusis]");
								await dialog.Talk("She has been practicing asceticism near ^000077Morocc Town, located SouthWest of Prontera City^000000.");
								break;
							case 4:
								await dialog.Talk("[Father Mareusis]");
								await dialog.Talk("Please visit ^000077 Father Yosuke ^000000 and return here to me.");
								await dialog.Next();
								await dialog.Talk("[Father Mareusis]");
								await dialog.Talk("He has been practicing asceticism near a ^000077bridge somewhere to the NorthWest of Prontera^000000.");
								break;
						}
						await dialog.Next();
						await dialog.Talk("[Father Mareusis]");
						await dialog.Talk("May the grace of God brighten your path and guide you on your journey of penance.");
						dialog.Close();
						return;
					}
					await dialog.Talk("Hmm...");
					await dialog.Talk("Your name is on the list and you've proven your qualification.");
					await dialog.Next();
					await dialog.Talk("[Father Mareusis]");
					await dialog.Talk("I am proud to say that you are now ready to become an Acolyte!");
					await dialog.Next();
					// UNHANDLED: skill "NV_TRICKDEAD",0,SKILL_PERM;
					// UNHANDLED: callfunc "Job_Change",Job_Acolyte;
					// UNHANDLED: callfunc "F_ClearJobVar";
					/**
					if (player.Quests.GetStatus(1001) != -1)
					{
						player.Quests.Complete(1001);
					}
					else if (player.Quests.GetStatus(1002) != -1)
					{
						player.Quests.Complete(1002);
					}
					else
					{
						player.Quests.Complete(1003);
					}
					**/
					await dialog.Talk("[Father Mareusis]");
					await dialog.Talk("Always remember to be thankful to God, who is taking care of us all the time.");
					await dialog.Next();
					await dialog.Talk("[Father Mareusis]");
					await dialog.Talk("Always use your gifts to serve Him by helping others. In chaos and times of difficulty, face your hardships with unwavering faith.");
					await dialog.Next();
					await dialog.Talk("[Father Mareusis]");
					await dialog.Talk("Lastly, I want to sincerely congratulate you on persevering through your trial of penance.");
					dialog.Close();
					return;
				case 2:
					await dialog.Talk("[Father Mareusis]");
					await dialog.Talk("Do you wish to become an Acolyte? You must fulfill two requirements.");
					await dialog.Next();
					await dialog.Talk("[Father Mareusis]");
					await dialog.Talk("First, you have to reach at least Novice Job Level 9 and learn all of the Basic Skills. Second, you will be given a trial of penance to overcome.");
					await dialog.Next();
					await dialog.Talk("[Father Mareusis]");
					if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) != 0)
					{
						switch (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0))
						{
							case 2:
								await dialog.Talk("For your trial, please visit ^000077Father Rubalkabara ^000000 and then return here to me.");
								await dialog.Next();
								await dialog.Talk("[Father Mareusis]");
								await dialog.Talk("He is practicing asceticism in the ^000077Relics at the NorthEast of Prontera City^000000.");
								break;
							case 3:
								await dialog.Talk("For your trial, please visit ^000077Mother Mathilda^000000 and return here to me.");
								await dialog.Next();
								await dialog.Talk("[Father Mareusis]");
								await dialog.Talk("She has been practicing asceticism near ^000077Morocc, located to the SouthWest of Prontera City^000000.");
								break;
							default:
								await dialog.Talk("For your trial, please visit ^000077Father Yosuke^000000 and return here to me.");
								await dialog.Next();
								await dialog.Talk("[Father Mareusis]");
								await dialog.Talk("He has been practicing asceticism around a bridge somewhere ^000077NorthWest of Prontera^000000.");
								break;
						}
						await dialog.Next();
						await dialog.Talk("[Father Mareusis]");
						await dialog.Talk("May the grace of God light your path and guide you on your journey of penance.");
					}
					else
					{
						await dialog.Talk("The destination for this trial will be decided once you fill the application form.");
					}
					await dialog.Next();
					await dialog.Talk("[Father Mareusis]");
					await dialog.Talk("Please come back after fulfilling the two requirements I've asked of you. As long as your desire to serve God and others is sincere, you will be able to make it.");
					dialog.Close();
					return;
				case 3:
					dialog.Close();
					return;
			}
		}
	}

	private async Task Ascetic_89Dialog(Dialog dialog)
	{
		var player = dialog.Player;
		{
			await dialog.Talk("[Father Rubalkabara]");
			if (player.Parameters.Get(ParameterType.BaseJob) == player.Vars.Perm.GetInt(VAR_JOB_NOVICE, 0))
			{
				if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) == 6)
				{
					await dialog.Talk("Please take care. They should know that you've met me by the time you arrive at the Prontera Sanctuary.");
					await dialog.Next();
					await dialog.Talk("[Father Rubalkabara]");
					await dialog.Talk("I've sent a carrier pigeon with a message. I hope it will arrive there safely...");
					dialog.Close();
					return;
				}
				if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) != 0)
				{
					if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) == 2)
					{
						await dialog.Talk("Oh...? You must be the one who aspires to become an Acolyte. I've already received news from the Sanctuary that you might be coming.");
						await dialog.Next();
						await dialog.Talk("[Father Rubalkabara]");
						await dialog.Talk($"Now, your name was {player.Name}, right? Excellent, thank you for visiting me.");
						await dialog.Next();
						await dialog.Talk("[Father Rubalkabara]");
						await dialog.Talk("I believe you've been told much about Acolytes from Friar Mareusis. Plus, there's plenty of helpful people in the Prontera Sanctuary.");
						await dialog.Next();
						await dialog.Talk("[Father Rubalkabara]");
						await dialog.Talk("I guess there's really no need for me to teach you much. Besides, I'm sure your someone from your generation may have trouble listening to an old man like me. Hahaha~");
						await dialog.Next();
						await dialog.Talk("[Father Rubalkabara]");
						await dialog.Talk("Still, lessons may come from the places you'd least expect. God loves to teach his children in strange ways. You'll see.");
						await dialog.Next();
						await dialog.Talk("[Father Rubalkabara]");
						await dialog.Talk("Well, I'll send the message telling them that you've come to visit me. So, you may now return to the Prontera Sanctuary.");
						await dialog.Next();
						await dialog.Talk("[Father Rubalkabara]");
						await dialog.Talk("Farewell.");
						dialog.Close();
						return;
						player.Warp("prt_fild03", 361, 255);
						player.Vars.Perm.Set(VAR_JOB_ACOLYTE_Q, 6);
						// 'end' can imply a warp or script termination, review needed.
						return;
					}
					else
					{
						await dialog.Talk("Oh...");
						await dialog.Talk("Are you one of the");
						await dialog.Talk("Acolyte applicants...?");
						await dialog.Talk("Let's see...");
						await dialog.Next();
						await dialog.Talk("[Father Rubalkabara]");
						await dialog.Talk($"Your name is {player.Name}?");
						await dialog.Talk("I don't think your name");
						await dialog.Talk("is on my list. Hmmm...");
						await dialog.Next();
						await dialog.Talk("[Father Rubalkabara]");
						await dialog.Talk("Why don't you go back to the Prontera Sanctuary and check again?");
						dialog.Close();
						return;
					}
				}
				else
				{
					await dialog.Talk("Huh? What brings you here? This is a very dangerous place for a Novice like yourself!");
					dialog.Close();
					return;
				}
			}
			else if (player.Parameters.Get(ParameterType.BaseJob) == player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE, 0))
			// UNHANDLED: else;
			{
				if (player.Parameters.Get(ParameterType.BaseJob) == player.Vars.Perm.GetInt(VAR_JOB_PRIEST, 0))
				{
					await dialog.Talk("Greetings.");
					await dialog.Next();
					await dialog.Talk("[Father Rubalkabara]");
					await dialog.Talk("Welcome to the Deep. Feel free to sit and contemplate God's message with me. This place is beautiful, even if danger accompanies its sense of serenity...");
					dialog.Close();
					return;
				}
				else
				{
					await dialog.Talk("Oh ho...");
					await dialog.Talk("Have you come into the Deep here for training? Or are you just a Wanderer?");
					await dialog.Next();
					await dialog.Talk("[Father Rubalkabara]");
					await dialog.Talk("Whoever you are, please take care of yourself. The monsters in here are shockingly strong, contrary to their cute appearance.");
					dialog.Close();
					return;
				}
			}
		}
	}

	private async Task Ascetic_95Dialog(Dialog dialog)
	{
		var player = dialog.Player;
		{
			await dialog.Talk("[Mother Mathilda]");
			if (player.Parameters.Get(ParameterType.BaseJob) == player.Vars.Perm.GetInt(VAR_JOB_NOVICE, 0))
			{
				if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) == 7)
				{
					await dialog.Talk("I will send a carrier pigeon to the Prontera Sanctuary. When you return, the Priest there should already have received my message.");
					await dialog.Next();
					await dialog.Talk("[Mother Mathilda]");
					await dialog.Talk("I will pray to God, and hope that you become an Acolyte soon.");
					dialog.Close();
					return;
				}
				if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) != 0)
				{
					if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) == 3)
					{
						await dialog.Talk("Ah, you must be one of the Acolyte applicants. I sincerely welcome you.");
						await dialog.Next();
						await dialog.Talk("[Mother Mathilda]");
						await dialog.Talk($"What is your name? {player.Name}? Let's see... Ah, you're on my list.");
						await dialog.Next();
						await dialog.Talk("[Mother Mathilda]");
						await dialog.Talk($"I will send a message to the Sanctuary confirming that you, {player.Name} visited me and completed your penance.");
						await dialog.Next();
						await dialog.Talk("[Mother Mathilda]");
						await dialog.Talk("Please return to the Prontera Sanctuary and speak to the Priest in charge.");
						dialog.Close();
						return;
						player.Warp("moc_fild07", 35, 355);
						player.Vars.Perm.Set(VAR_JOB_ACOLYTE_Q, 7);
						// 'end' can imply a warp or script termination, review needed.
						return;
					}
					else
					{
						await dialog.Talk("Ah...!");
						await dialog.Talk("You must be one");
						await dialog.Talk("of the Acolyte applicants.");
						await dialog.Talk("I sincerely welcome you.");
						await dialog.Next();
						await dialog.Talk("[Mother Mathilda]");
						await dialog.Talk("Now, what is your name?");
						await dialog.Talk($"{player.Name}? Let's see...");
						await dialog.Next();
						await dialog.Talk("[Mother Mathilda]");
						await dialog.Talk("Hmm...");
						await dialog.Talk("It seems your name");
						await dialog.Talk("is not on my list...");
						await dialog.Next();
						await dialog.Talk("[Mother Mathilda]");
						await dialog.Talk("Perhaps you should return to the Prontera Sanctuary and check the destination for your penance trial once again.");
						dialog.Close();
						return;
					}
				}
				else
				{
					await dialog.Talk("...");
					dialog.Close();
				}
			}
			else if (player.Parameters.Get(ParameterType.BaseJob) == player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE, 0))
			// UNHANDLED: else;
			{
				if (player.Parameters.Get(ParameterType.BaseJob) == player.Vars.Perm.GetInt(VAR_JOB_PRIEST, 0))
				{
					await dialog.Talk("Hello there~");
					await dialog.Next();
					await dialog.Talk("[Mother Mathilda]");
					await dialog.Talk("How is your practice coming along? I certainly hope you're enjoying living in the grace of God.");
					dialog.Close();
				}
				else
				{
					await dialog.Talk("May God");
					await dialog.Talk("be with you...");
					dialog.Close();
				}
			}
		}
	}

	private async Task Ascetic_98Dialog(Dialog dialog)
	{
		var player = dialog.Player;
		{
			await dialog.Talk("[Father Yosuke]");
			if (player.Parameters.Get(ParameterType.BaseJob) == player.Vars.Perm.GetInt(VAR_JOB_NOVICE, 0))
			{
				if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) == 8)
				{
					await dialog.Talk("What?");
					await dialog.Next();
					await dialog.Talk("[Father Yosuke]");
					await dialog.Talk("Have you any more business with me?! You don't! Go back to the Sanctuary now!");
					dialog.Close();
					return;
				}
				if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) != 0)
				{
					if (player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE_Q, 0) == 4)
					{
						await dialog.Talk("Hey.");
						await dialog.Talk("Whatever you are,");
						await dialog.Talk("you look like an");
						await dialog.Talk("Acolyte applicant.");
						await dialog.Talk("Right?");
						await dialog.Next();
						await dialog.Talk("[Father Yosuke]");
						await dialog.Talk("Not bad, not bad. You withstood the penance trial pretty well.");
						await dialog.Talk("So what's your name?");
						await dialog.Next();
						await dialog.Talk("[Father Yosuke]");
						await dialog.Talk($"{player.Name}, huh?");
						await dialog.Next();
						await dialog.Talk("[Father Yosuke]");
						await dialog.Talk($"Okay. I'll send a message to the Sanctuary that you, {player.Name}, came to visit me.");
						await dialog.Next();
						await dialog.Talk("[Father Yosuke]");
						await dialog.Talk("Now go back to the Santuary and finish becoming an Acolyte, kid.");
						dialog.Close();
						player.Warp("prt_fild00", 206, 230);
						player.Vars.Perm.Set(VAR_JOB_ACOLYTE_Q, 8);
						// 'end' can imply a warp or script termination, review needed.
					}
					else
					{
						await dialog.Talk("Hey.");
						await dialog.Talk("You look like an Acolyte Applicant. Am I right?");
						await dialog.Next();
						await dialog.Talk("[Father Yosuke]");
						await dialog.Talk("Not bad at all, you've made it all the way here from Prontera. So what's your name, kid?");
						await dialog.Next();
						await dialog.Talk("[Father Yosuke]");
						await dialog.Talk($"{player.Name}, huh? Why isn't your name on my list?");
						await dialog.Next();
						await dialog.Talk("[Father Yosuke]");
						await dialog.Talk("You probably made a mistake. Go back to the Santuary, and check with the Bishop.");
						dialog.Close();
					}
				}
				else
				{
					await dialog.Talk("You...");
					await dialog.Talk("Novice.");
					await dialog.Talk("There something");
					await dialog.Talk("you wanna tell me?");
					dialog.Close();
				}
			}
			else if (player.Parameters.Get(ParameterType.BaseJob) == player.Vars.Perm.GetInt(VAR_JOB_ACOLYTE, 0))
			// UNHANDLED: else;
			{
				if (player.Parameters.Get(ParameterType.BaseJob) == player.Vars.Perm.GetInt(VAR_JOB_PRIEST, 0))
				{
					await dialog.Talk("Hey...");
					await dialog.Next();
					await dialog.Talk("[Father Yosuke]");
					await dialog.Talk("If you like, come sit here with me and meditate the great truths. God's majesty is truly inspiring...");
					dialog.Close();
				}
				else
				{
					await dialog.Talk("Do you have anything to say? Because unfortunately for you,");
					await dialog.Talk("I don't any replies.");
					dialog.Close();
				}
			}
		}
	}
}
