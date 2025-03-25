namespace labyrinth;

/* Enumerated type representing one of the four ports leaving a MazeCell. */
public enum Port
{
    NORTH,
    SOUTH,
    EAST,
    WEST,
    Undefined
};

public class MazeUtilities
{
    /**
     * Given a location in a maze, returns whether the given sequence of
     * steps will let you escape the maze. The steps should be given as
     * a string made from N, S, E, and W for north/south/east/west without
     * spaces or other punctuation symbols, such as "WESNNNS"
     * <p>
     * To escape the maze, you need to find the Potion, the Spellbook, and
     * the Wand. You can only take steps in the four cardinal directions,
     * and you can't move in directions that don't exist in the maze.
     * <p>
     * It's assumed that the input MazeCell is not null.
     *
     * @param start The start location in the maze.
     * @param moves The sequence of moves.
     * @return Whether that sequence of moves picks up the needed items
     *         without making nay illegal moves.
     */
    public static bool isPathToFreedom(MazeCell start, String moves)
    {
        MazeCell? curr = start;
        HashSet<Item> items = new HashSet<Item>();

        /* Fencepost issue: pick up items from starting location, if any. */
        if (start.WhatsHere != Item.Nothing)
            items.Add(start.WhatsHere);

        foreach (char ch in moves.ToCharArray())
        {
            /* Take a step. */
            if (ch == 'N') curr = curr.North;
            else if (ch == 'S') curr = curr.South;
            else if (ch == 'E') curr = curr.East;
            else if (ch == 'W') curr = curr.West;
            else return false; // Unknown character?

            /* Was that illegal? */
            if (curr == null) return false;

            /* Did we get anything? */
            if (curr.WhatsHere != Item.Nothing)
                items.Add(curr.WhatsHere);
        }

        /* Do we have all three items? */
        return items.Count == 3;
    }

    /* Simple rolling hash. Stolen shameless from StanfordCPPLib, maintained by a collection
     * of talented folks at Stanford University. We use this hash implementation to ensure
     * consistency from run to run and across systems.
     */
    private static readonly int HASH_SEED = 5381;       // Starting point for first cycle
    private static readonly int HASH_MULTIPLIER = 33;   // Multiplier for each cycle
    private static readonly int HASH_MASK = 0x7FFFFFFF; // All 1 bits except the sign

    private static int hashCode(int value)
    {
        return value & HASH_MASK;
    }

    private static int hashCode(String str)
    {
        int hash = HASH_SEED;
        foreach (char ch in str.ToCharArray())
        {
            hash = HASH_MULTIPLIER * hash + ch;
        }
        return hashCode(hash);
    }

    /*
     * Computes a composite hash code from a list of multiple values.
     * The components are scaled up so as to spread out the range of values
     * and reduce collisions.
     */
    private static int hashCode(String str, int[] values)
    {
        int result = hashCode(str);
        foreach (int value in values)
        {
            result = result * HASH_MULTIPLIER + value;
        }
        return hashCode(result);
    }

    /* Size of a normal maze. */
    private static readonly int NUM_ROWS = 4;
    private static readonly int NUM_COLS = 4;

    /* Size of a twisty maze. */
    private static readonly int TWISTY_MAZE_SIZE = 12;

    /**
     * Returns a maze specifically tailored to the given name.
     *
     * We've implemented this function for you. You don't need to write it
     * yourself.
     *
     * Please don't make any changes to this function - we'll be using our
     * reference version when testing your code, and it would be a shame if
     * the maze you solved wasn't the maze we wanted you to solve!
     */
    public static MazeCell mazeFor(String name)
    {
        /* Java Random is guaranteed to produce the same sequence of values across
         * all systems with the same seed.
         */
        int seed = hashCode(name, new[] { NUM_ROWS, NUM_COLS });
        Random generator = new Random(seed);
        MazeCell[,] maze = makeMaze(NUM_ROWS, NUM_COLS, generator);

        List<MazeCell> linearMaze = new List<MazeCell>();
        for (int row = 0; row < maze.GetLength(0); row++)
        {
            for (int col = 0; col < maze.GetLength(1); col++)
            {
                linearMaze.Add(maze[row,col]);
            }
        }

        int[,] distances = allPairsShortestPaths(linearMaze);
        int[] locations = remoteLocationsIn(distances);

        /* Place the items. */
        linearMaze[locations[1]].WhatsHere = Item.Spellbook;
        linearMaze[locations[2]].WhatsHere = Item.Potion;
        linearMaze[locations[3]].WhatsHere = Item.Wand;

        /* We begin in position 0. */
        return linearMaze[locations[0]];
    }

    /**
     * Returns a twisty maze specifically tailored to the given name.
     *
     * Please don't make any changes to this function - we'll be using our
     * reference version when testing your code, and it would be a shame if
     * the maze you solved wasn't the maze we wanted you to solve!
     */
    public static MazeCell twistyMazeFor(String name)
    {
        /* Java Random is guaranteed to produce the same sequence of values across
         * all systems with the same seed.
         */
        Random generator = new Random(hashCode(name, new int[]{TWISTY_MAZE_SIZE}));
        List<MazeCell> maze = makeTwistyMaze(TWISTY_MAZE_SIZE, generator);

        /* Find the distances between all pairs of nodes. */
        int[,] distances = allPairsShortestPaths(maze);

        /* Select a 4-tuple maximizing the minimum distances between points,
         * and use that as our item/start locations.
         */
        int[] locations = remoteLocationsIn(distances);

        /* Place the items there. */
        maze[locations[1]].WhatsHere = Item.Spellbook;
        maze[locations[2]].WhatsHere = Item.Potion;
        maze[locations[3]].WhatsHere = Item.Wand;

        return maze[locations[0]];
    }

    /* Returns if two nodes are adjacent. */
    private static bool areAdjacent(MazeCell first, MazeCell second)
    {
        return first.East == second ||
               first.West == second ||
               first.North == second ||
               first.South == second;
    }

    /* Uses the Floyd-Warshall algorithm to compute the shortest paths between all
     * pairs of nodes in the maze. The result is a table where table[i][j] is the
     * shortest path distance between maze[i] and maze[j].
     */
    private static int[,] allPairsShortestPaths(List<MazeCell> maze)
    {
        /* Floyd-Warshall algorithm. Fill the grid with "infinity" values. */
        int[,] result = new int[maze.Count, maze.Count];
        for (int i = 0; i < result.GetLength(0); i++)
        {
            for (int j = 0; j < result.GetLength(1); j++)
            {
                result[i, j] = maze.Count + 1;
            }
        }

        /* Set distances of nodes to themselves at 0. */
        for (int i = 0; i < maze.Count; i++)
        {
            result[i, i] = 0;
        }

        /* Set distances of edges to 1. */
        for (int i = 0; i < maze.Count; i++)
        {
            for (int j = 0; j < maze.Count; j++)
            {
                if (areAdjacent(maze[i], maze[j]))
                {
                    result[i, j] = 1;
                }
            }
        }

        /* Dynamic programming step. Keep expanding paths by allowing for paths
         * between nodes.
         */
        for (int i = 0; i < maze.Count; i++)
        {
            int[,] next = new int[maze.Count,maze.Count];
            for (int j = 0; j < maze.Count; j++)
            {
                for (int k = 0; k < maze.Count; k++)
                {
                    next[j, k] = Math.Min(result[j, k], result[j, i] + result[i, k]);
                }
            }
            result = next;
        }

        return result;
    }

    /* Given a list of distinct nodes, returns the "score" for their distances,
     * which is a sequence of numbers representing pairwise distances in sorted
     * order.
     */
    private static List<int> scoreOf(int[] nodes, int[,] distances)
    {
        List<int> result = new List<int>();

        for (int i = 0; i < nodes.Length; i++)
        {
            for (int j = i + 1; j < nodes.Length; j++)
            {
                result.Add(distances[nodes[i],nodes[j]]);
            }
        }

        result.Sort();
        return result;
    }

    /* Lexicographical comparison of two arrays; they're assumed to have the same length. */
    private static bool lexicographicallyFollows(List<int> lhs, List<int> rhs)
    {
        for (int i = 0; i < lhs.Count; i++)
        {
            if (lhs[i] != rhs[i]) return lhs[i] > rhs[i];
        }
        return false;
    }

    /* Given a grid, returns a combination of four nodes whose overall score
     * (sorted list of pairwise distances) is as large as possible in a
     * lexicographical sense.
     */
    private static int[] remoteLocationsIn(int[,] distances)
    {
        int[] result = new int[] { 0, 1, 2, 3 };

        /* We could do this recursively, but since it's "only" four loops
         * we'll just do that instead. :-)
         */
        for (int i = 0; i < distances.GetLength(0); i++)
        {
            for (int j = i + 1; j < distances.GetLength(0); j++)
            {
                for (int k = j + 1; k < distances.GetLength(0); k++)
                {
                    for (int l = k + 1; l < distances.GetLength(0); l++)
                    {
                        int[] curr = new int[] { i, j, k, l };
                        if (lexicographicallyFollows(scoreOf(curr, distances), scoreOf(result, distances)))
                        {
                            result = curr;
                        }
                    }
                }
            }
        }

        return result;
    }

    /* Clears all the links between the given group of nodes. */
    private static void clearGraph(List<MazeCell> nodes)
    {
        foreach (MazeCell node in nodes)
        {
            node.WhatsHere = Item.Nothing;
            node.North = node.South = node.East = node.West = null;
        }
    }

    /* Returns a random unassigned link from the given node, or nullptr if
     * they are all assigned.
     */
    private static Port randomFreePortOf(MazeCell cell, Random generator)
    {
        List<Port> ports = new List<Port>();
        if (cell.East == null) ports.Add(Port.EAST);
        if (cell.West == null) ports.Add(Port.WEST);
        if (cell.North == null) ports.Add(Port.NORTH);
        if (cell.South == null) ports.Add(Port.SOUTH);
        if (ports.Any() == false) return Port.Undefined;

        int port = generator.Next(ports.Count);
        return ports[port];
    }

    /* Links one MazeCell to the next using the specified port. */
    private static void link(MazeCell from, MazeCell to, Port link)
    {
        switch (link)
        {
            case Port.EAST:
                from.East = to;
                break;
            case Port.WEST:
                from.West = to;
                break;
            case Port.NORTH:
                from.North = to;
                break;
            case Port.SOUTH:
                from.South = to;
                break;
            default:
                throw new ApplicationException("Unknown port.");
        }
    }

    /* Use a variation of the Erdos-Renyi random graph model. We set the
     * probability of any pair of nodes being connected to be ln(n) / n,
     * then artificially constrain the graph so that no node has degree
     * four or more. We generate mazes this way until we find one that's
     * conencted.
     */
    private static bool erdosRenyiLink(List<MazeCell> nodes, Random generator)
    {
        /* High probability that everything is connected. */
        double threshold = Math.Log(nodes.Count) / nodes.Count;

        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                if (generator.NextDouble() <= threshold)
                {
                    Port iLink = randomFreePortOf(nodes[i], generator);
                    Port jLink = randomFreePortOf(nodes[j], generator);

                    /* Oops, no free links. */
                    if (iLink == Port.Undefined || jLink == Port.Undefined)
                    {
                        return false;
                    }

                    link(nodes[i], nodes[j], iLink);
                    link(nodes[j], nodes[i], jLink);
                }
            }
        }

        return true;
    }

    /* Returns whether the given maze is connected. Uses a BFS. */
    private static bool isConnected(List<MazeCell> maze)
    {
        HashSet<MazeCell> visited = new HashSet<MazeCell>();
        LinkedList<MazeCell> frontier = new LinkedList<MazeCell>();

        frontier.AddLast(maze[0]);

        while (frontier.Any())
        {
            MazeCell curr = frontier.Last();
            frontier.RemoveLast();

            if (!visited.Contains(curr))
            {
                visited.Add(curr);

                if (curr.East != null) frontier.AddLast(curr.East);
                if (curr.West != null) frontier.AddLast(curr.West);
                if (curr.North != null) frontier.AddLast(curr.North);
                if (curr.South != null) frontier.AddLast(curr.South);
            }
        }

        return visited.Count == maze.Count;
    }

    /* Generates a random twisty maze. This works by repeatedly generating
     * random graphs until a connected one is found.
     */
    private static List<MazeCell> makeTwistyMaze(int numNodes, Random generator)
    {
        List<MazeCell> result = new List<MazeCell>();
        for (int i = 0; i < numNodes; i++)
        {
            result.Add(new MazeCell());
        }

        /* Keep generating mazes until we get a connected one. */
        do
        {
            clearGraph(result);
        } while (!erdosRenyiLink(result, generator) || !isConnected(result));

        return result;
    }


    /* Returns all possible edges that could appear in a grid maze. */
    private static List<EdgeBuilder> allPossibleEdgesFor(MazeCell[,] maze)
    {
        List<EdgeBuilder> result = new List<EdgeBuilder>();
        for (int row = 0; row < maze.GetLength(0); row++)
        {
            for (int col = 0; col < maze.GetLength(1); col++)
            {
                if (row + 1 < maze.GetLength(0))
                {
                    result.Add(new EdgeBuilder(maze[row, col], maze[row + 1, col], Port.SOUTH, Port.NORTH));
                }
                if (col + 1 < maze.GetLength(1))
                {
                    result.Add(new EdgeBuilder(maze[row, col], maze[row, col + 1], Port.EAST, Port.WEST));
                }
            }
        }
        return result;
    }

    /* Union-find FIND operation. */
    private static MazeCell repFor(Dictionary<MazeCell, MazeCell> reps, MazeCell cell)
    {
        while (reps[cell] != cell)
        {
            cell = reps[cell];
        }
        return cell;
    }

    /* Shuffles the edges using the Fischer-Yates shuffle. */
    private static void shuffleEdges(List<EdgeBuilder> edges, Random generator)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            int j = generator.Next(edges.Count - i) + i;

            EdgeBuilder temp = edges[i];
            edges[i] = edges[j];
            edges[j] = temp;
        }
    }

    /* Creates a random maze of the given size using a randomized Kruskal's
     * algorithm. Edges are shuffled and added back in one at a time, provided
     * that each insertion links two disconnected regions.
     */
    private static MazeCell[,] makeMaze(int numRows, int numCols, Random generator)
    {
        MazeCell[,] maze = new MazeCell[numRows, numCols];
        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                maze[row, col] = new MazeCell();
            }
        }

        List<EdgeBuilder> edges = allPossibleEdgesFor(maze);
        shuffleEdges(edges, generator);

        /* Union-find structure, done without path compression because N is small. */
        Dictionary<MazeCell, MazeCell> representatives = new Dictionary<MazeCell, MazeCell>();
        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                MazeCell elem = maze[row, col];
                representatives[elem] = elem;
            }
        }

        /* Run a randomized Kruskal's algorithm to build the maze. */
        int edgesLeft = numRows * numCols - 1;
        for (int i = 0; edgesLeft > 0 && i < edges.Count; i++)
        {
            EdgeBuilder edge = edges[i];

            /* See if they're linked already. */
            MazeCell rep1 = repFor(representatives, edge.from);
            MazeCell rep2 = repFor(representatives, edge.to);

            /* If not, link them. */
            if (rep1 != rep2)
            {
                representatives[rep1] = rep2;

                link(edge.from, edge.to, edge.fromPort);
                link(edge.to, edge.from, edge.toPort);

                edgesLeft--;
            }
        }
        if (edgesLeft != 0) throw new ApplicationException("Edges remain?"); // Internal error!

        return maze;
    }

}

/* Type representing an edge between two maze cells. */
public sealed class EdgeBuilder
{
    public MazeCell from { get; set; }
    public MazeCell to { get; set; }

    public Port fromPort { get; set; }
    public Port toPort { get; set; }

    public EdgeBuilder(MazeCell from, MazeCell to, Port fromPort, Port toPort)
    {
        this.from = from;
        this.to = to;
        this.fromPort = fromPort;
        this.toPort = toPort;
    }
}


