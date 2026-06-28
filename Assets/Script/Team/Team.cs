using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Team", menuName = "SlimeTeam/Team")]
public class Team : ScriptableObject
{
    public List<Slime> team = new List<Slime>();
}
