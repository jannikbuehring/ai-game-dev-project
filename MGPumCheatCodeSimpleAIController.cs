using System.Collections;
using System.Collections.Generic;
using mg.pummelz;
using UnityEngine;

public class MGPumCheatCodeSimpleAIController : MGPumStudentAIPlayerController
{

    public const string type = "CC SIMPLE";

    private List<Vector2Int> directions = null;

    private List<Vector2Int> getDirections()
    {
        if(directions == null)
        {
            directions = new List<Vector2Int>();
            directions.Add(Vector2Int.left);
            directions.Add(Vector2Int.right);
            directions.Add(Vector2Int.up);
            directions.Add(Vector2Int.down);
            directions.Add(Vector2Int.left + Vector2Int.up);
            directions.Add(Vector2Int.left + Vector2Int.down);
            directions.Add(Vector2Int.right + Vector2Int.up);
            directions.Add(Vector2Int.right + Vector2Int.down);
        }
        return directions;
    }

    public MGPumCheatCodeSimpleAIController(int playerID) : base(playerID)
    {
    }

    internal override MGPumCommand calculateCommand() {

        int enemyID = 1 - playerID;

        //List<MGPumUnit> possibleMovers = new List<MGPumUnit>;

        foreach (MGPumUnit unit in state.getAllUnitsInZone(MGPumZoneType.Battlegrounds, this.playerID))
        {
            if (stateOracle.canAttack(unit) && unit.currentRange > 0)
            {
                MGPumField goal = state.getField(unit.field.coords + Vector2Int.up);

                if (goal != null && goal.unit != null && goal.unit.ownerID == enemyID) {
                    
                    MGPumAttackChainMatcher matcher = unit.getAttackMatcher();

                    MGPumFieldChain chain = new MGPumFieldChain(this.playerID, matcher);
                    chain.add(unit.field);
                    chain.add(goal);

                    MGPumAttackCommand command = new MGPumAttackCommand(playerID, chain, unit);
                    return command;
                }
            }
            
            if (stateOracle.canMove(unit) && unit.currentSpeed > 0) 
            {
                //possibleMovers.Add(unit);

                MGPumField goal = state.getField(unit.field.coords + Vector2Int.up);

                if (goal != null) {
                    if (goal.unit == null) {
                        MGPumMoveChainMatcher matcher = unit.getMoveMatcher();

                        MGPumFieldChain chain = new MGPumFieldChain(this.playerID, matcher);
                        chain.add(unit.field);
                        chain.add(goal);
                        MGPumMoveCommand command = new MGPumMoveCommand(this.playerID, chain, unit);
                        return command;
                    }
                }
            }
        }
    return new MGPumEndTurnCommand(this.playerID);
    }

    protected override int[] getTeamMartikels()
    {
        return new int[]{4757202, 2923947};
    }
}    


