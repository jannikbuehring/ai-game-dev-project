using System.Collections;
using System.Collections.Generic;
using mg.pummelz;
using UnityEngine;

public class MGPumCheatCodeAIPlayerController : MGPumStudentAIPlayerController
{
    /*

    AUFGABE 3

    */

    public const string type = "Cheat Code";

    private bool[,] queued;
    private SortedSet<Vector2Int> tilesToVisit;
    private Dictionary<Vector2Int, Vector2Int> predecessors;
    private Dictionary<Vector2Int, int> distance;
    private List<Vector2Int> touchedTiles;

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

    public MGPumCheatCodeAIPlayerController(int playerID) : base(playerID)
    {
    }

    internal override MGPumCommand calculateCommand() {

        foreach (MGPumUnit unit in state.getAllUnitsInZone(MGPumZoneType.Battlegrounds, this.playerID))
        {
            if (stateOracle.canMove(unit))
            {
                List<MGPumField> possibleFields = getPossibleFields(unit);
                //Debug.Log("Unit:" + unit);
                //Debug.Log("Count:" + possibleFields.Count);

                List<MGPumMoveCommand> moveCommands = new List<MGPumMoveCommand>();

                int bestMoveScore = 0;
                MGPumMoveCommand bestMoveCommand = null;

                foreach(MGPumField field in possibleFields)
                {
                    // find path to field
                    MGPumMoveCommand moveCommand = findPath(field, unit);
                    
                    // only add path if one was found
                    if (moveCommand != null) 
                    {
                        moveCommands.Add(moveCommand);
                    }
                }

                // check if there were any possible move commands
                if(moveCommands.Count > 0)
                {
                    foreach (MGPumMoveCommand mc in moveCommands)
                    {
                        // score move
                        int score = scoreMove(mc);
                        
                        if (score >= bestMoveScore)
                        {
                            bestMoveScore = score;
                            bestMoveCommand = mc;
                        }
                    }

                    if (bestMoveCommand != null)
                    {
                        //Debug.Log("Best move score: " + bestMoveScore);
                        //Debug.Log("Best move: " + bestMoveCommand);
                        //Debug.Log("---------------------");
                        return bestMoveCommand;
                    }
                    
                }
            }

            if (stateOracle.canAttack(unit))
            {
                List<MGPumField> possibleEnemyFields = getPossibleEnemyFields(unit, state, 0);
                //Debug.Log(unit);
                //Debug.Log(possibleEnemyFields.Count);

                List<MGPumAttackCommand> attackCommands = new List<MGPumAttackCommand>();
                int bestAttackScore = 0;
                MGPumAttackCommand bestAttackCommand = null;

                foreach (MGPumField enemyField in possibleEnemyFields)
                {
                    MGPumAttackCommand attackCommand = findAttack(enemyField, unit, state);
                    //Debug.Log(attackCommand);

                    if (attackCommand != null)
                    {
                        attackCommands.Add(attackCommand);
                    }
                }

                if(attackCommands.Count > 0)
                {
                    foreach (MGPumAttackCommand ac in attackCommands)
                    {
                        int score = scoreAttack(ac);
                        if (score >= bestAttackScore)
                        {
                            bestAttackScore = score;
                            bestAttackCommand = ac;
                        }
                    }
                    
                    if (bestAttackCommand != null)
                    {
                        //Debug.Log("Best Command Score: " + bestAttackScore);
                        return bestAttackCommand;
                    }

                    
                }
            }      
        }

        return new MGPumEndTurnCommand(this.playerID);
    }

    // for a given unit, get a list of all the possible fields in moving range of the unit
    List<MGPumField> getPossibleFields(MGPumUnit unit)
    {
        List<MGPumField> possibleFields = new List<MGPumField>();

        Vector2Int position = unit.field.coords;

        MGPumField field = new MGPumField(unit.coords.x, unit.coords.y);

        for (int horizontal = 0; horizontal <= unit.currentSpeed; horizontal++)
        {
            for (int vertical = 0; vertical <= unit.currentSpeed; vertical++)
            {
                // check every direction
                if (state.getField(position + Vector2Int.up * vertical + Vector2Int.left * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.up * vertical + Vector2Int.left * horizontal);
                    if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && state.getUnitForField(field) == null)
                    {
                        possibleFields.Add(field);
                    }
                }

                if (state.getField(position + Vector2Int.up * vertical + Vector2Int.right * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.up * vertical + Vector2Int.right * horizontal);
                    if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && state.getUnitForField(field) == null)
                    {
                        possibleFields.Add(field);
                    }
                }

                if (state.getField(position + Vector2Int.down * vertical + Vector2Int.left * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.down * vertical + Vector2Int.left * horizontal);
                    if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && state.getUnitForField(field) == null)
                    {
                        possibleFields.Add(field);
                    }
                }

                if (state.getField(position + Vector2Int.down * vertical + Vector2Int.right * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.down * vertical + Vector2Int.right * horizontal);
                    if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && state.getUnitForField(field) == null)
                    {
                        possibleFields.Add(field);
                    }
                }
            }
        }

        return possibleFields;
    }

    // get a list of fields where enemy units are standing
    List<MGPumField> getPossibleEnemyFields(MGPumUnit unit, MGPumGameState state, int vision)
    {
        List<MGPumField> possibleFields = new List<MGPumField>();

        Vector2Int position = unit.field.coords;

        MGPumField field = new MGPumField(unit.coords.x, unit.coords.y);
        MGPumUnit foundUnit = null;

        for (int horizontal = 0; horizontal <= unit.currentRange + vision; horizontal++)
        {
            for (int vertical = 0; vertical <= unit.currentRange + vision; vertical++)
            {
                if (state.getField(position + Vector2Int.up * vertical + Vector2Int.left * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.up * vertical + Vector2Int.left * horizontal);
                    foundUnit = state.getUnitForField(field);
                    if (foundUnit != null)
                    {
                        if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && foundUnit.ownerID == 1 - unit.ownerID)
                        {
                        possibleFields.Add(field);
                        }
                    }                   
                }

                if (state.getField(position + Vector2Int.up * vertical + Vector2Int.right * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.up * vertical + Vector2Int.right * horizontal);
                    foundUnit = state.getUnitForField(field);
                    if (foundUnit != null)
                    {
                        if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && foundUnit.ownerID == 1 - unit.ownerID)
                        {
                            possibleFields.Add(field);
                        }
                    }
                }

                if (state.getField(position + Vector2Int.down * vertical + Vector2Int.left * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.down * vertical + Vector2Int.left * horizontal);
                    foundUnit = state.getUnitForField(field);
                    if (foundUnit != null)
                    {
                        if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && foundUnit.ownerID == 1 - unit.ownerID)
                        {
                            possibleFields.Add(field);
                        }
                    }
                }

                if (state.getField(position + Vector2Int.down * vertical + Vector2Int.right * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.down * vertical + Vector2Int.right * horizontal);
                    foundUnit = state.getUnitForField(field);
                    if (foundUnit != null)
                    {
                        if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && foundUnit.ownerID == 1 - unit.ownerID)
                        {
                            possibleFields.Add(field);
                        }
                    }
                }
            }
        }

        return possibleFields;
    }

    // return a list of fields where allied units are standing
    List<MGPumField> getAllyFields(MGPumUnit unit, MGPumGameState state)
    {
        List<MGPumField> possibleFields = new List<MGPumField>();

        Vector2Int position = unit.field.coords;

        MGPumField field = new MGPumField(unit.coords.x, unit.coords.y);
        MGPumUnit foundUnit = null;

        for (int horizontal = 0; horizontal <= unit.currentSpeed; horizontal++)
        {
            for (int vertical = 0; vertical <= unit.currentSpeed; vertical++)
            {
                if (state.getField(position + Vector2Int.up * vertical + Vector2Int.left * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.up * vertical + Vector2Int.left * horizontal);
                    foundUnit = state.getUnitForField(field);
                    if (foundUnit != null)
                    {
                        if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && foundUnit.ownerID == unit.ownerID)
                        {
                        possibleFields.Add(field);
                        }
                    }                   
                }

                if (state.getField(position + Vector2Int.up * vertical + Vector2Int.right * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.up * vertical + Vector2Int.right * horizontal);
                    foundUnit = state.getUnitForField(field);
                    if (foundUnit != null)
                    {
                        if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && foundUnit.ownerID == unit.ownerID)
                        {
                            possibleFields.Add(field);
                        }
                    }
                }

                if (state.getField(position + Vector2Int.down * vertical + Vector2Int.left * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.down * vertical + Vector2Int.left * horizontal);
                    foundUnit = state.getUnitForField(field);
                    if (foundUnit != null)
                    {
                        if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && foundUnit.ownerID == unit.ownerID)
                        {
                            possibleFields.Add(field);
                        }
                    }
                }

                if (state.getField(position + Vector2Int.down * vertical + Vector2Int.right * horizontal) != null)
                {
                    field = state.getField(position + Vector2Int.down * vertical + Vector2Int.right * horizontal);
                    foundUnit = state.getUnitForField(field);
                    if (foundUnit != null)
                    {
                        if (!possibleFields.Contains(field) && field.coords != position && state.fields.inBounds(field.coords) && foundUnit.ownerID == unit.ownerID)
                        {
                            possibleFields.Add(field);
                        }
                    }
                }
            }
        }

        return possibleFields;
    }

    //find path for moving and return it
    MGPumMoveCommand findPath (MGPumField field, MGPumUnit unit) 
    {
        // can we find this out in a better way?
        queued = new bool[8, 8];
        tilesToVisit = new SortedSet<Vector2Int>(new AStarComparer(field.coords));
        predecessors = new Dictionary<Vector2Int, Vector2Int>();
        //distance = new Dictionary<Vector2Int, int>();

        tilesToVisit.Add(unit.field.coords);
        queued[unit.field.coords.x, unit.field.coords.y] = true;

        int recursion = 0;
        int maxRecursion = 500;
        while(tilesToVisit.Count > 0)
        {
            recursion++;
            if(recursion > maxRecursion)
            {
                break;
            }

            Vector2Int position = tilesToVisit.Min;
            tilesToVisit.Remove(position);

            if (!state.fields.inBounds(position))
            {
                continue;
            }

            //touchedTiles.Add(position);

            if (position == field.coords)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                path.Add(position);

                //reconstruct path in reverse
                while(predecessors.ContainsKey(path[0]))
                {
                    path.Insert(0, predecessors[path[0]]);
                }

                MGPumMoveChainMatcher matcher = unit.getMoveMatcher();

                MGPumFieldChain chain = new MGPumFieldChain(unit.ownerID, matcher);

                for (int i = 0; i < path.Count; i++)
                {
                    chain.add(state.getField(path[i]));
                }

                if (chain.isValidChain())
                {
                    MGPumMoveCommand mc = new MGPumMoveCommand(this.playerID, chain, unit);
                    return mc;
                }

                continue;
            }

            foreach (Vector2Int direction in getDirections())
            {
                Vector2Int neighbor = position + direction;

                if (!state.fields.inBounds(neighbor))
                {
                    continue;
                }

                if(!queued[neighbor.x, neighbor.y] && state.getUnitForField(state.getField(neighbor)) == null)
                {
                    queued[neighbor.x, neighbor.y] = true;
                    tilesToVisit.Add(neighbor);
                    predecessors.Add(neighbor, position);
                    //distance.Add(neighbor, )
                }
            }
        }
        return null;
    }

    // find possible attack and return it
    MGPumAttackCommand findAttack (MGPumField field, MGPumUnit unit, MGPumGameState state) 
    {
        // can we find this out in a better way?
        queued = new bool[8, 8];
        tilesToVisit = new SortedSet<Vector2Int>(new AStarComparer(field.coords));
        predecessors = new Dictionary<Vector2Int, Vector2Int>();
        //distance = new Dictionary<Vector2Int, int>();

        tilesToVisit.Add(unit.field.coords);
        queued[unit.field.coords.x, unit.field.coords.y] = true;

        int recursion = 0;
        int maxRecursion = 500;
        while(tilesToVisit.Count > 0)
        {
            recursion++;
            if(recursion > maxRecursion)
            {
                break;
            }

            Vector2Int position = tilesToVisit.Min;
            tilesToVisit.Remove(position);

            if (!state.fields.inBounds(position))
            {
                continue;
            }

            //touchedTiles.Add(position);

            if (position == field.coords)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                path.Add(position);

                //reconstruct path in reverse
                while(predecessors.ContainsKey(path[0]))
                {
                    path.Insert(0, predecessors[path[0]]);
                }

                MGPumAttackChainMatcher matcher = unit.getAttackMatcher();

                MGPumFieldChain chain = new MGPumFieldChain(unit.ownerID, matcher);

                for (int i = 0; i < path.Count; i++)
                {
                    chain.add(state.getField(path[i]));
                }

                if (chain.isValidChain())
                {
                    MGPumAttackCommand ac = new MGPumAttackCommand(this.playerID, chain, unit);
                    return ac;
                }

                continue;
            }

            foreach (Vector2Int direction in getDirections())
            {
                Vector2Int neighbor = position + direction;

                if (!state.fields.inBounds(neighbor))
                {
                    continue;
                }

                if(!queued[neighbor.x, neighbor.y])
                {
                    queued[neighbor.x, neighbor.y] = true;
                    tilesToVisit.Add(neighbor);
                    predecessors.Add(neighbor, position);
                    //distance.Add(neighbor, )
                }
            }
        }
        return null;
    }

    // method for scoring attacks
    private int scoreAttack(MGPumAttackCommand attack)
    {
        int score = 0;

        // prio list
        //enemy unit dies
        if (attack.attacker.currentPower > attack.defender.currentHealth)
        {
            // enemy king dies
            if(attack.defender.unitID == "PUM008")
            {
                score += 100000;
            }

            //if we can kill a unit with a higher range its good
            if (attack.attacker.currentRange <= attack.defender.currentRange)
            {
                score += 3;
            }

            score += 5;

            // "strong" enemy unit dies
            // "weak" enemy unit dies
            score += (scoreUnit(attack.defender) * 2);
        }
        else
        {
            score += scoreUnit(attack.defender);
        }
        
        // attack from max range
        if (attack.attacker.currentRange == attack.chain.getLength() - 1)
        {
            score += 5;
        }
        // enemy range < our range
        if (attack.attacker.currentRange > attack.defender.currentRange)
        {
            score += 3;
        }
        

        //Debug.Log(attack);
        //Debug.Log("Defender Unit id: " + attack.defender.unitID);
        //Debug.Log("Score: " + score);
        return score;
    }

    // method for evaluating how strong a unit is
    private int scoreUnit(MGPumUnit unit)
    {
        return unit.currentRange + unit.currentSpeed + unit.currentPower;
    }
    
    // method to score a move command
    private int scoreMove(MGPumMoveCommand move)
    {
        int score = 0;
        
        MGPumGameState copiedState = state.deepCopy();

        MGPumAIPlayerDummyController dummy1 = new MGPumAIPlayerDummyController(playerID);
        MGPumAIPlayerDummyController dummy2 = new MGPumAIPlayerDummyController(1 - playerID);

        MGPumGameController copiedController = new MGPumGameController(dummy1, dummy2, copiedState);

        copiedController.acceptCommand(move);

        MGPumUnit copiedUnit = null;

        if (copiedController.state.getUnitForField(move.chain.getLast()) != null)
        {
            copiedUnit = copiedController.state.getUnitForField(move.chain.getLast());
        }
        else
        {
            // this should never happen, but just in case we give it a really bad score
            return -1000;
        }

        // get fields for different vision ranges
        List<MGPumField> possibleEnemyFields = getPossibleEnemyFields(copiedUnit, copiedState, 0);
        List<MGPumField> possibleEnemyFieldsExtraRange = getPossibleEnemyFields(copiedUnit, copiedState, 2);
        List<MGPumField> possibleEnemyFieldsExtraRangeKing = getPossibleEnemyFields(copiedUnit, copiedState, 1);
        List<MGPumField> allyFields = getAllyFields(copiedUnit, copiedState);

        foreach (MGPumField allyField in allyFields)
        {
            // if Bummz is nearby, reduce score
            if (state.getUnitForField(allyField).unitID == "PUM007")
            {
                score -= 5;
            }
        }


        //Debug.Log("Move: " + move);
        //Debug.Log("Possible Enemy Fields Count: " + possibleEnemyFields.Count);

        // if buffy or haley find a move to reach a lot of allies, score will be increased
        if (state.getUnitForField(move.chain.getFirst()).unitID == "PUM013" || state.getUnitForField(move.chain.getFirst()).unitID == "PUM014")
        {
            score += (allyFields.Count * 3);
        }
        // if unit is bummz, get away from teammates and go towards enemies
        if (state.getUnitForField(move.chain.getFirst()).unitID == "PUM007")
        {
            score += (possibleEnemyFieldsExtraRange.Count * 5);
            score -= (allyFields.Count * 2);
        }
        else if (state.getUnitForField(move.chain.getFirst()).unitID == "PUM004")
        {
            // Wolli should tank, sending him towards enemies rewards points
            score += (possibleEnemyFieldsExtraRange.Count * 3);
        }
        else if (state.getUnitForField(move.chain.getFirst()).unitID == "PUM008")
        {
            // the more wounded our king is, the higher the penalty for stepping near enemies
            score -= (possibleEnemyFieldsExtraRangeKing.Count * (state.getUnitForField(move.chain.getFirst()).damage + 1));
        }
        else
        {
            // the more enemies in attack range of the unit, the higher the penalty
            score -= (possibleEnemyFields.Count * 3);
        }

        // method in case no prior weighting has been given
        if (score == 0)
        {
            Debug.Log("Score ist 0");
            List<MGPumField> allEnemyFields = getPossibleEnemyFields(copiedUnit, copiedState, 7);
            foreach (MGPumField enemyField in allEnemyFields)
            {
                // move towards enemies
                if (Vector2Int.Distance(move.chain.getFirst().coords, enemyField.coords) > Vector2Int.Distance(move.chain.getLast().coords, enemyField.coords))
                {
                score += 1;
                break;
                }
            }
        }
        
        //Debug.Log("Score sollte hier negativ sein: " + score);

        List<MGPumAttackCommand> attackCommands = new List<MGPumAttackCommand>();
        int bestAttackScore = 0;
        MGPumAttackCommand bestAttackCommand = null;

        foreach (MGPumField enemyField in possibleEnemyFields)
        {
            MGPumAttackCommand attackCommand = findAttack(enemyField, copiedUnit, copiedState);
            //Debug.Log(attackCommand);

            if (attackCommand != null)
            {
                attackCommands.Add(attackCommand);
            }
        }

        if(attackCommands.Count > 0)
        {
            foreach (MGPumAttackCommand ac in attackCommands)
            {
                int attackScore = scoreAttack(ac);
                if (attackScore >= bestAttackScore)
                {
                    bestAttackScore = attackScore;
                    bestAttackCommand = ac;
                }
            }
        }

        // final score is based on movement score and attack score
        score += bestAttackScore;
        
        //Debug.Log("Best Move Score: " + bestAttackScore);
        //Debug.Log("Best Move: "+ bestAttackCommand);
        return score;
    }

    protected override int[] getTeamMartikels()
    {
        return new int[]{4757202, 2923947};
    }
}    

/*
public class AStarComparer : Comparer<Vector2Int>
    {

        private Vector2Int goal;

        public AStarComparer(Vector2Int goal)
        {
            this.goal = goal;
        }

        public float calculateFScore(Vector2Int item) {
            return Vector2Int.Distance(item, goal);
        }

        public override int Compare(Vector2Int a, Vector2Int b)
        {
            float aScore = calculateFScore(a);
            float bScore = calculateFScore(b);

            int comp = aScore.CompareTo(bScore);

            if (comp == 0)
            {
                comp = a.x.CompareTo(b.x);
            }
            if (comp == 0)
            {
                comp = a.y.CompareTo(b.y);
            }
            return comp;
        }

}*/