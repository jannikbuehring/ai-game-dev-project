using System.Collections;
using System.Collections.Generic;
using mg.pummelz;
using UnityEngine;

public class MGPumCheatCodeRandomAIController : MGPumStudentAIPlayerController
{

    public const string type = "CC Random";

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

    public MGPumCheatCodeRandomAIController(int playerID) : base(playerID)
    {
    }

    internal override MGPumCommand calculateCommand() {

        foreach (MGPumUnit unit in state.getAllUnitsInZone(MGPumZoneType.Battlegrounds, this.playerID))
        {
            if (stateOracle.canAttack(unit))
            {
                List<MGPumField> possibleEnemyFields = getPossibleEnemyFields(unit);
                //Debug.Log(unit);
                //Debug.Log(possibleEnemyFields.Count);

                List<MGPumAttackCommand> attackCommands = new List<MGPumAttackCommand>();

                foreach (MGPumField enemyField in possibleEnemyFields)
                {
                    MGPumAttackCommand attackCommand = findAttack(enemyField, unit);
                    //Debug.Log(attackCommand);

                    if (attackCommand != null)
                    {
                        attackCommands.Add(attackCommand);
                    }
                }

                if(attackCommands.Count > 0)
                {
                    int index = rng.Next(attackCommands.Count);
                    //Debug.Log(attackCommands[index]);
                    return attackCommands[index];
                }
            }           

            if (stateOracle.canMove(unit))
            {
                List<MGPumField> possibleFields = getPossibleFields(unit);
                //Debug.Log("Unit:" + unit);
                //Debug.Log("Count:" + possibleFields.Count);

                List<MGPumMoveCommand> moveCommands = new List<MGPumMoveCommand>();

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
                    int index = rng.Next(moveCommands.Count);
                    //Debug.Log(moveCommands[index]);
                    return moveCommands[index];
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
    List<MGPumField> getPossibleEnemyFields(MGPumUnit unit)
    {
        List<MGPumField> possibleFields = new List<MGPumField>();

        Vector2Int position = unit.field.coords;

        MGPumField field = new MGPumField(unit.coords.x, unit.coords.y);
        MGPumUnit foundUnit = null;

        for (int horizontal = 0; horizontal <= unit.currentRange; horizontal++)
        {
            for (int vertical = 0; vertical <= unit.currentRange; vertical++)
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
    MGPumAttackCommand findAttack (MGPumField field, MGPumUnit unit) 
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
    

    protected override int[] getTeamMartikels()
    {
        return new int[]{4757202, 2923947};
    }
}    

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

}

