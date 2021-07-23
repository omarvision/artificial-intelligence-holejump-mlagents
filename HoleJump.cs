using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

// Behaviour Parameters:
//  vector observation = neural network inputs
//  vector action = neural network outputs

public class HoleJump : Agent
{
    public float Movespeed = 20f;
    public float Jumpforce = 1.5f;
    public int cntFloorX = 8;
    public int cntFloorZ = 6;
    public GameObject prefabFloor = null;
    public float PercentFloorHoles = 0.15f;

    private Bounds bndFloor;
    private GameObject Target = null;
    private bool onGround = true;
    private Rigidbody rb = null;
    private bool left;
    private bool right;
    private bool back;
    private bool forward;
    private bool down;

    public override void Initialize()
    {
        bndFloor = prefabFloor.GetComponent<Renderer>().bounds;
        Target = this.transform.parent.transform.Find("Target").gameObject;
        rb = this.GetComponent<Rigidbody>();

        Globals.ScreenText();

        MakeFloor();
    }
    public override void OnEpisodeBegin()
    {
        MakeFloor();
        Globals.Episode += 1;
    }
    public override void OnActionReceived(float[] vectorAction)
    {
        //move
        this.transform.Translate(Vector3.right * vectorAction[0] * Movespeed * Time.deltaTime);
        this.transform.Translate(Vector3.forward * vectorAction[1] * Movespeed * Time.deltaTime);
              
        //jump
        if (onGround == true && vectorAction[2] != 0)
        {
            rb.AddForce(Vector3.up * vectorAction[2] * Jumpforce, ForceMode.VelocityChange);
        }

        OffFloorCheck();
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        //Note: these are the pieces of information I determined the agent needs to know (sense) to avoid holes and reach target

        //floor feelers 
        Vector3 p = this.transform.position;
        left = Physics.Raycast(new Vector3(p.x - 0.5f, p.y, p.z), Vector3.down + (Vector3.left * 0.5f));
        right = Physics.Raycast(new Vector3(p.x + 0.5f, p.y, p.z), Vector3.down + (Vector3.right * 0.5f));
        back = Physics.Raycast(new Vector3(p.x, p.y, p.z - 0.5f), Vector3.down + (Vector3.back * 0.5f));
        forward = Physics.Raycast(new Vector3(p.x, p.y, p.z + 0.5f), Vector3.down + (Vector3.forward * 0.5f));
        down = Physics.Raycast(p, Vector3.down);
        sensor.AddObservation(left);                        // +1
        sensor.AddObservation(right);                       // +1
        sensor.AddObservation(back);                        // +1
        sensor.AddObservation(forward);                     // +1   
        sensor.AddObservation(down);                        // +1 = 5

        //position of agent
        sensor.AddObservation(this.transform.position);     // +3 = 8

        //direction agent to target
        Vector3 direction = Vector3.Normalize(this.transform.position - Target.transform.position);
        sensor.AddObservation(direction);                   // +3 = 11

        //position of target
        sensor.AddObservation(Target.transform.position);   // +3 = 14 total observations

        Globals.ScreenText(string.Format("down: {0}  {1},{2},{3},{4}", down, left, right, back, forward));
    }
    public override void Heuristic(float[] actionsOut)
    {
        //Heuristic: is a good way to test you actions to see if they work as you would expect
        actionsOut[0] = 0;  //movement x axis   
        actionsOut[1] = 0;  //movement z axis
        actionsOut[2] = 0;  //jump

        if (Input.GetKey(KeyCode.LeftArrow) == true)
            actionsOut[0] = -1;
        if (Input.GetKey(KeyCode.RightArrow) == true)
            actionsOut[0] = 1;

        if (Input.GetKey(KeyCode.UpArrow) == true)
            actionsOut[1] = 1;
        if (Input.GetKey(KeyCode.DownArrow) == true)
            actionsOut[1] = -1;

        if (Input.GetKey(KeyCode.Space) == true)
            actionsOut[2] = 1;
    }
    private void MakeFloor()
    {
        //delete all floor pieces
        for (int i = this.transform.parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = this.transform.parent.GetChild(i).gameObject;
            if (child.CompareTag("Floor") == true)
            {
                GameObject.Destroy(child);
            }
        }

        Vector3 offset = new Vector3(this.transform.parent.position.x, this.transform.parent.position.y, this.transform.parent.position.z);

        bool flgAgent = false;
        bool flgTarget = false;
        for (int x = 0; x < cntFloorX; x++)
        {
            for (int z = 0; z < cntFloorZ; z++)
            {
                if (Random.Range(0, 100) > PercentFloorHoles * 100)
                {
                    //create floor
                    GameObject obj = Instantiate(prefabFloor, this.transform.parent);
                    obj.name = string.Format("floor{0}_{1}", x, z);
                    obj.transform.position = new Vector3(offset.x + x * bndFloor.size.x, offset.y - 1, offset.z + z * bndFloor.size.z);

                    if (flgAgent == false && Random.Range(0, 100) < 10)
                    {
                        //place agent
                        this.transform.position = new Vector3(offset.x + x * bndFloor.size.x, offset.y, offset.z + z * bndFloor.size.z);
                        flgAgent = true;
                    }
                    else if (flgTarget == false && Random.Range(0, 100) < 10)
                    {
                        //place target
                        Target.transform.position = new Vector3(offset.x + x * bndFloor.size.x, offset.y, offset.z + z * bndFloor.size.z);
                        flgTarget = true;
                    }
                }
            }
        }
    }
    private void OffFloorCheck()
    {
        //nothing below agent, and lower than the floor
        if (down == false && this.transform.position.y < 0) 
        {
            Globals.Fail += 1;
            AddReward(-0.1f);
            EndEpisode();
        }
    }
    private void FixedUpdate()
    {   
        if (rb.velocity.y < 0)  
        {
            rb.velocity += Vector3.up * Physics.gravity.y * 2.5f * Time.deltaTime;  //slam down jump
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target") == true)
        {
            Globals.Success += 1;
            AddReward(1.0f);
            EndEpisode();
        }

        if (collision.gameObject.CompareTag("Floor") == true)
        {
            onGround = true;
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor") == true)
        {
            onGround = false;
        }
    }
    private void OnDrawGizmos()
    {
        Vector3 p = this.transform.position;

        //floor sensors (left, right, back, forward)
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(new Vector3(p.x - 0.5f, p.y, p.z), Vector3.down + (Vector3.left * 0.5f));    
        Gizmos.DrawRay(new Vector3(p.x + 0.5f, p.y, p.z), Vector3.down + (Vector3.right * 0.5f));
        Gizmos.DrawRay(new Vector3(p.x, p.y, p.z - 0.5f), Vector3.down + (Vector3.back * 0.5f)); 
        Gizmos.DrawRay(new Vector3(p.x, p.y, p.z + 0.5f), Vector3.down + (Vector3.forward * 0.5f));

        // down
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(p, Vector3.down);
    }
}
