using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
/**
 * 起こりえない状態
 * F L R U D
 * 0 0 0 1 1  3
 * 0 0 1 1 1  7
 * 0 1 0 1 1  11
 * 0 1 1 0 0  12
 * 0 1 1 0 1  13
 * 0 1 1 1 0  14
 * 0 1 1 1 1  15
 * 1 0 0 1 1  19
 * 1 0 1 1 1  23
 * 1 1 0 1 1  27
 * 1 1 1 0 0  28
 * 1 1 1 0 1  29
 * 1 1 1 1 0  30
 * 1 1 1 1 1  31
 * 
 */

public class avoidWallMoveCreature : MonoBehaviour
{
    private Transform myTransform;

    public float angleLimits;
    public float speed;

    private int tryMax = 500; //tryの最大値
    private int episodeMax = 10000; //episode最大値
    private double[] meanRewards; //meanReward保存配列
    private int[] tryCount;
    private int testInterval = 50; //テストする間隔
    private int testTryCounter = 0; //テスト回数
    private int testTryMax = 100; //テスト回数
    private int testState = 0; //0:終了（なにもなし）．1:開始，2:中

    private const double ConstToReduceSpeed = 0.001f; // speedから座標の増分に変換する比例定数
    private const double ConstToSpeed2Frame = 1000; // speedと，1行動で進む距離から，フレーム数を算出するための比例定数

    private double anglePerOneAct = 30; // 1行動で曲がれる距離
    private double coordinatePerOneAct = 1; // 1行動で進む距離
    private double wallWarningThrDis = 8; // 壁が近いと判断する距離の閾値
    private float worldScale = 40; // 世界のでかさ，箱の一辺の長さ
    private float myScale = 0.5f; // 自分の正面のサイズ

    private int actFrameCounter = 0;
    private bool actProcRunning = false;

    private bool alive = true; // 生きているかどうか．falseはエピソードの終了を示す．

    public bool collisionDetect = false;

    //Variables related to Reinforcement Learning
    private ActSelUtility act;
    private QLearning agent;

    private StatesElement Status; // 現在の状態
    private StatesElement n_Status; // アクション後の状態
    private int nowAction; // 今行うアクション，更新前に行ったアクション
    private int nextAction; // 次行うアクション，更新後，すなわち次のターンに行うアクション，sarsaで使う
    private double Reward = 0; // 報酬
    private double RewardMean = 0; // 報酬平均

    private int tryCounter = 0;
    private int episodeCounter = 0;

    private enum Actions
    {
        //指定した角度方向に前進
        R,
        L,
        U,
        D,
        //前進
        F
    }

    private struct StatesElement
    {
        public int distToWallFromF;//前みた壁の距離 近い1,近くない0
        public int distToWallFromL;//左からの距離
        public int distToWallFromR;//右からの距離
        public int distToWallFromU;//上からの距離
        public int distToWallFromD;//下からの距離

        //半分くらいいらない（到達しえない）状態　やり方考えた方がよさそう　気になるなら
        public static readonly int[] limit = { 2, 2, 2, 2, 2 };

        public int convertIndex()
        {
            return this.distToWallFromF * StatesElement.limit[3] * StatesElement.limit[2] * StatesElement.limit[1] * StatesElement.limit[0]
                 + this.distToWallFromL * StatesElement.limit[2] * StatesElement.limit[1] * StatesElement.limit[0]
                 + this.distToWallFromR * StatesElement.limit[1] * StatesElement.limit[0]
                 + this.distToWallFromU * StatesElement.limit[0]
                 + this.distToWallFromD;
        }

        public StatesElement(int x, int y, int z, int w, int q)
        {
            this.distToWallFromF = x;
            this.distToWallFromL = y;
            this.distToWallFromR = z;
            this.distToWallFromU = w;
            this.distToWallFromD = q;
        }
    }

    private Vector3[] DoFSVectors =
    {
    Quaternion.Euler(   0,   0,   0) * new Vector3(1, 0, 0), //前
    Quaternion.Euler(   0, -90,   0) * new Vector3(1, 0, 0), //左
    Quaternion.Euler(   0,  90,   0) * new Vector3(1, 0, 0), //右
    Quaternion.Euler(   0,   0,  90) * new Vector3(1, 0, 0), //上
    Quaternion.Euler(   0,   0, -90) * new Vector3(1, 0, 0), //下
    //Quaternion.Euler(   0, 180,   0) * new Vector3(1, 0, 0), //後 動けないから意味ないかも
    };

    // debug用の変数群
    private int[] statusCounter;
    string foldPath = "creatureAvoidWall\\Assets\\QvalueFile";
    bool folE = false;
    bool debugOff = true; //今のQ値でDebugしない→true いじらない
    bool QdebugOn = false; //Q値を使うかどうか ここだけいじる
    DateTime dt;

    // Start is called before the first frame update
    void Start()
    {
        myTransform = this.transform;

        //define states and action
        act = new ActSelUtility();
        // 行動可能な行動数の配列，行動の種類数で初期化
        int slen = 1;
        foreach (var x in StatesElement.limit) slen *= x; // StatesElementを使う場合

        // 行動bindの設定 
        int[] aBind = Enumerable.Repeat(Enum.GetNames(typeof(Actions)).Length, slen).ToArray();
        //bind無し

        //agent = new QLearning(0.2f, 0.99f,
        //                       aBind, 100f,
        //                       ActSelUtility.RANDOM, ActSelUtility.GREEDY);

        agent = new QLearning(0.2f, 0.99f,
                               aBind, 100f,
                               ActSelUtility.EPSGREEDY, ActSelUtility.GREEDY);
        agent.fixActionSelector(0.1f, 0.4f);

        //agent = new QLearning(0.2f, 0.99f,
        //                       aBind, 100f,
        //                       ActSelUtility.SOFTMAX, ActSelUtility.GREEDY);
        //agent.fixActionSelector(0.1f, 0.4f);

        //初期状態の取得
        StaterForStatesElement(ref Status);
        episodeCounter += 1;

        //各状態に遭遇した回数を保存する配列
        statusCounter = Enumerable.Repeat(0, slen).ToArray();

        //事前に用意したQをロードする
        if (QdebugOn)
        {
            agent.loadQ(foldPath + "\\Q.txt");
            debugOff = false;
            agent.switchPolicy();
        }

        tryCount = new int[episodeMax / testInterval];
    }

    // Update is called once per frame
    void Update()
    {
        //方策の切り替え
        if (Input.GetKeyDown(KeyCode.A) && !QdebugOn)
        {
            agent.switchPolicy();
            debugOff = !debugOff;
        }

        //episode数（死んだ回数）の表示
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("episode : " + episodeCounter);
        }

        //try数（行動回数）の表示
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("try : " + tryCounter + " [episode : " + episodeCounter + "]");
        }

        //リセット
        if (Input.GetKeyDown(KeyCode.R) && QdebugOn)
        {
            collisionDetect = true;
        }

        //行動選択 行動中でなければ実行
        if (!actProcRunning)
        {
            statusCounter[Status.convertIndex()] += 1;
            nowAction = agent.SelectAction(Status.convertIndex()); // 制限される可能性のある行動が，列挙された一番最後の行動であればこの書き方でよい．だが，異なる場合は，受け取った行動のインデックスからさらに処理を加える必要あり
            tryCounter += 1;
        }
        // 行動の実行
        if (alive) controlMoving((Actions)nowAction);

        //壁に当たって死んだとき
        if (collisionDetect)
        {
            alive = false;
            actProcRunning = false;
            collisionDetect = false;
            transform.tag = "corpse";
        }

        //死んでいたら-50
        if (!alive) Reward = -50;
        else Reward = 0;

        // 今のUpdate()の実行時，行動が終了していた(actProcRunning = falseになった)場合
        if (!actProcRunning)
        {
            //行動後の状態の取得
            StaterForStatesElement(ref n_Status);

            //Q値の更新
            if (debugOff)
            {
                agent.ValueUpdate(Status.convertIndex(), nowAction, n_Status.convertIndex(), Reward);
            }
            
            //状態の更新
            Status = n_Status;
        }

        if (!alive || tryCounter == tryMax)
        {
            DEATHtroy();
            if (testTryCounter == testTryMax)
            {
                agent.switchPolicy();
                debugOff = true;
                testState = 0;
            }
            else if (testState == 1)
            {
                agent.switchPolicy();
                debugOff = false;
                testState = 2;
            }
            else if (episodeCounter % testInterval == 0) testState = 1;
        }

        //時間付きデバッグ
        //dt = DateTime.Now;
        //Debug.Log(dt.Minute.ToString() + "分" + dt.Second.ToString() + "秒" + dt.Millisecond.ToString() + "ミリ秒" + ":" + Lifepoint.ToString());
    }

    // 状態の取得
    private void StaterForStatesElement(ref StatesElement s)
    {
        Vector4 playerPos = new Vector4(myTransform.position.x, myTransform.position.y, myTransform.position.z, 1); // (自分の座標, 1)

        Vector3[] nVector = new Vector3[6]; // 各壁の法線ベクトル 箱の内側を向いている
        //探索可能範囲の空間の形を直方体と仮定する．
        nVector[0] = new Vector3(0, 1, 0); //floorの法線ベクトル
        nVector[1] = new Vector3(0, 0, -1); //wall1の法線ベクトル正面から奥
        nVector[2] = new Vector3(1, 0, 0); //wall2の法線ベクトル左
        nVector[3] = new Vector3(-1, 0, 0); //wall3の法線ベクトル右
        nVector[4] = new Vector3(0, 0, 1); //wall4の法線ベクトル手前
        nVector[5] = new Vector3(0, -1, 0); //ceilingの法線ベクトル

        Vector4[] equationParam = new Vector4[6]; // 各壁の方程式のパラメータa,b,c,d
        // 平面の方程式は aX + bY + cZ + d = 0
        // 平面の法線ベクトル n = (a,b,c) , 平面のある点 p0 = (x0,y0,z0) , 平面上の任意の点 p = (x,y,z) とすると，
        // 方程式は ax + by + cz - (ax0 + by0 + cz0) = 0
        // equationParam[i] = ( a, b, c, d )
        equationParam[0] = new Vector4(nVector[0].x, nVector[0].y, nVector[0].z, -Vector3.Dot(nVector[0], GameObject.Find("Floor").transform.position)); //floorの方程式
        equationParam[1] = new Vector4(nVector[1].x, nVector[1].y, nVector[1].z, -Vector3.Dot(nVector[1], GameObject.Find("wall1").transform.position)); //wall1の方程式
        equationParam[2] = new Vector4(nVector[2].x, nVector[2].y, nVector[2].z, -Vector3.Dot(nVector[2], GameObject.Find("wall2").transform.position)); //wall2の方程式
        equationParam[3] = new Vector4(nVector[3].x, nVector[3].y, nVector[3].z, -Vector3.Dot(nVector[3], GameObject.Find("wall3").transform.position)); //wall3の方程式
        equationParam[4] = new Vector4(nVector[4].x, nVector[4].y, nVector[4].z, -Vector3.Dot(nVector[4], GameObject.Find("wall4").transform.position)); //wall4の方程式
        equationParam[5] = new Vector4(nVector[5].x, nVector[5].y, nVector[5].z, -Vector3.Dot(nVector[5], GameObject.Find("ceiling").transform.position)); //ceilingの方程式

        // 自分から見た5方向のベクトル
        Vector3[] myVVector = new Vector3[DoFSVectors.Length];
        for (int i = 0; i < DoFSVectors.Length; i++)
            myVVector[i] = Quaternion.Euler(myTransform.eulerAngles) * DoFSVectors[i];

        // 内積がこれ以上の場合は，計算しない
        // 内積は逆を向くとマイナスになる，直角は0
        // まず，内積っていうのは単位ベクトル同士だったらcosと同じになる．
        // 一番角小さくなる時として，自分の正面の大きさの半分の長さを底辺とし，斜辺を箱の長さとする直角三角形を考える
        // こいつのcosを求め，これを閾値とする．
        float dotThr = - (myScale / 2) / worldScale;

        // 自分の進行方向にない壁は無視する
        double[] minDir = new double[DoFSVectors.Length];
        double t = worldScale;
        for (int i = 0; i < DoFSVectors.Length; i++)
        {
            t = worldScale;
            minDir[i] = t;
            for (int j = 0; j < 6; j++)
            {
                // 自分から見た各方向について内積を計算
                if (Vector3.Dot(myVVector[i], nVector[j]) < dotThr)
                {
                    //この距離の計算は，式をいじればわかるはずさ，自分で解け
                    //ある点から，ある方向に進んだ際に，ある平面と交わるまでの距離を求めている．
                    t = -Vector4.Dot(playerPos, equationParam[j]) / Vector3.Dot(myVVector[i], nVector[j]);
                    // 自分と同じ方向であり，今までの最小値である時
                    if (t >= 0 && minDir[i] > t)
                        minDir[i] = t;
                }
            }
        }

        s.distToWallFromF = minDir[0] <= wallWarningThrDis ? 1 : 0;
        s.distToWallFromL = minDir[1] <= wallWarningThrDis ? 1 : 0;
        s.distToWallFromR = minDir[2] <= wallWarningThrDis ? 1 : 0;
        s.distToWallFromU = minDir[3] <= wallWarningThrDis ? 1 : 0;
        s.distToWallFromD = minDir[4] <= wallWarningThrDis ? 1 : 0;
    }

    //死んだときの処理
    private void DEATHtroy()
    {
        if (debugOff) Debug.Log("died   try : " + tryCounter + ", RewardMean : " + RewardMean + " [episode : " + episodeCounter + "]  max:" + Max(statusCounter) + ", min:" + Min(statusCounter));
        else
        {
            tryCount[episodeCounter / testInterval] = (testTryCounter * tryCount[episodeCounter / testInterval] + tryCounter) / (1 + testTryCounter);
            Debug.Log("died   try : " + tryCounter + " tryMean:" + tryCount[episodeCounter / testInterval] + " max:" + Max(statusCounter) + ", min:" + Min(statusCounter));
        }

        if (!debugOff)
        {
            testTryCounter += 1;
        }
        else
        {
            episodeCounter += 1;
            testTryCounter = 0;
        }

        //あらゆる状態のリセット
        transform.tag = "origin";
        tryCounter = 0;
        actFrameCounter = 0;
        actProcRunning = false;
        alive = true;
        collisionDetect = false;

        // 各壁付近に一定の角度で出現させ，うまい具合に状態に出合わせる確率を均一にする．
        Vector3[] nVector = new Vector3[6]; // 各壁の法線ベクトル 箱の内側を向いている
        //探索可能範囲の空間の形を直方体と仮定する．
        nVector[0] = new Vector3(0, 1, 0); //floorの法線ベクトル
        nVector[1] = new Vector3(0, 0, -1); //wall1の法線ベクトル正面から奥
        nVector[2] = new Vector3(1, 0, 0); //wall2の法線ベクトル左
        nVector[3] = new Vector3(-1, 0, 0); //wall3の法線ベクトル右
        nVector[4] = new Vector3(0, 0, 1); //wall4の法線ベクトル手前
        nVector[5] = new Vector3(0, -1, 0); //ceilingの法線ベクトル
        
        Vector3[] locationCoordinates = new Vector3[6]; // 各壁の位置座標
        locationCoordinates[0] = GameObject.Find("Floor").transform.position;   //floorの位置座標
        locationCoordinates[1] = GameObject.Find("wall1").transform.position;   //wall1の位置座標
        locationCoordinates[2] = GameObject.Find("wall2").transform.position;   //wall2の位置座標
        locationCoordinates[3] = GameObject.Find("wall3").transform.position;   //wall3の位置座標
        locationCoordinates[4] = GameObject.Find("wall4").transform.position;   //wall4の位置座標
        locationCoordinates[5] = GameObject.Find("ceiling").transform.position; //ceilingの位置座標

        Vector4[] equationParam = new Vector4[6]; // 各壁の方程式のパラメータa,b,c,d
        // 平面の方程式は aX + bY + cZ + d = 0
        // 平面の法線ベクトル n = (a,b,c) , 平面のある点 p0 = (x0,y0,z0) , 平面上の任意の点 p = (x,y,z) とすると，
        // 方程式は ax + by + cz - (ax0 + by0 + cz0) = 0
        // equationParam[i] = ( a, b, c, d )
        for (int i = 0; i < 6; i++) equationParam[i] = new Vector4(nVector[i].x, nVector[i].y, nVector[i].z, -Vector3.Dot(nVector[i], locationCoordinates[i])); //各壁の方程式
        
        //法線方向を0,0,0として，壁がある方向
        Vector3[] dirVecList =
        {
            new Vector3(0, -90,   0), //左
            new Vector3(0,  90,   0), //右
            new Vector3(0,   0,  90), //上
            new Vector3(0,   0, -90)  //下
        };
        //各壁が背後にあるとき，0,0,0から何度方向にあるか
        Vector3[] dirAngList =
        {
            new Vector3(0,   0,  90), //floor
            new Vector3(0,  90,   0), //wall1
            new Vector3(0,   0,   0), //wall2
            new Vector3(0, 180,   0),  //wall3
            new Vector3(0, -90,   0), //wall4
            new Vector3(0,   0, -90)  //ceiling
        };
        //壁から離す距離
        int distanceFromWall = 5;

        // 壁番号
        int nearestWall = UnityEngine.Random.Range((int)0, (int)5);
        // 壁がある方向 後，前　以外　（前は一番出やすい，後は使わない）
        int wallExistDir = UnityEngine.Random.Range((int)0, (int)3);

        // マスク         (1,1,1) - 法線ベクトルが1のところ = 法線以外が0になる
        Vector3 vecMask = new Vector3(1f, 1f, 1f) - new Vector3(nVector[nearestWall].x * nVector[nearestWall].x,
                                                                nVector[nearestWall].y * nVector[nearestWall].y,
                                                                nVector[nearestWall].z * nVector[nearestWall].z);
        Vector3 pos;
        // vecMaskが0のところは0になる
        pos.x = vecMask.x * UnityEngine.Random.Range(distanceFromWall + (-worldScale / 2), (worldScale / 2) - distanceFromWall);
        pos.y = vecMask.y * UnityEngine.Random.Range(distanceFromWall, worldScale - distanceFromWall);
        pos.z = vecMask.z * UnityEngine.Random.Range(distanceFromWall + (-worldScale / 2), (worldScale / 2) - distanceFromWall);

        // distanceFromWallだけ離す
        pos += distanceFromWall * nVector[nearestWall];
        Vector3 direction = dirAngList[nearestWall] + dirVecList[wallExistDir];
        
        transform.position = pos;
        transform.eulerAngles = direction;

        //Q値のファイル書き込み
        if (episodeCounter % 500 == 0 && debugOff)
        {
            //folder作成
            if (!folE)
            {
                folE = true;
                dt = DateTime.Now;
                foldPath += dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString() + "_" + dt.Hour.ToString() + "_" + dt.Minute.ToString() + "_" + dt.Second.ToString() + "_" + dt.Millisecond.ToString();
                Directory.CreateDirectory(foldPath);
            }

            string senten = "";
            for (int i = 0; i < statusCounter.Length; i++)
            {
                senten += statusCounter[i].ToString() + ",";
            }
            senten = senten.Substring(0, senten.Length - 1);

            string path = foldPath + "/Qvalue-episodeCounter_" + episodeCounter + ".txt";
            string path2 = foldPath + "/tryCount-episodeCounter_" + episodeCounter + ".txt";
            // 書き込み
            File.WriteAllText(path, agent.QvaluesView() + "\n\nstatus encounters counters\n" + senten);
            File.WriteAllText(path2, String.Join(",", tryCount));
            Debug.Log("Save at " + path);

            // 追記
            //File.AppendAllText(path, "fuga");
            //Debug.Log("Save at " + path);

            // 読み込み
            //string data = File.ReadAllText(path);
            //Debug.Log("Data is " + data);
        }

        //初期状態の取得
        StaterForStatesElement(ref Status);
    }

    private void controlMoving(Actions actCmd)
    {
        // 1行動あたりのフレーム数を計算
        double framesPerOneAct = coordinatePerOneAct * (ConstToSpeed2Frame / Mathf.Abs(speed));
        // 向きを変える場合の角速度を計算
        double agnleSpeed = anglePerOneAct / framesPerOneAct;

        //現在の位置と顔の向きを取得
        Vector3 pos = myTransform.position;
        Vector3 direction = myTransform.eulerAngles;

        // 1行動で進むベクトルの大きさ speedに比例させる，ConstToReduceSpeed倍して減らす．
        float power = (float)ConstToReduceSpeed * Mathf.Abs(speed);

        actProcRunning = true;
        actFrameCounter += 1;

        //　各コマンドにおけるベクトルの計算
        if (actProcRunning) switch (actCmd)
            {
                //ある方向に前進
                case Actions.R:
                    direction.x += 0;
                    direction.y += (float)agnleSpeed;
                    direction.z += 0;
                    break;
                case Actions.L:
                    direction.x += 0;
                    direction.y -= (float)agnleSpeed;
                    direction.z += 0;
                    break;
                case Actions.U:
                    direction.x += 0;
                    direction.y += 0;
                    direction.z += (float)agnleSpeed;
                    break;
                case Actions.D:
                    direction.x += 0;
                    direction.y += 0;
                    direction.z -= (float)agnleSpeed;
                    break;
                //前進
                case Actions.F:
                    direction.x += 0;
                    direction.y += 0;
                    direction.z += 0;
                    break;
                default:
                    Debug.Log("act error");
                    actFrameCounter = (int)framesPerOneAct;
                    power = 0;
                    break;
            }

        // 1フレーム後の位置と顔の向きの計算
        Vector3 vec = new Vector3(power, 0, 0);
        Vector3 deltapos = Quaternion.Euler(direction.x, direction.y, direction.z) * vec;
        Vector3 frontVector = Quaternion.Euler(direction.x, direction.y, direction.z) * new Vector3(1, 0, 0); // 正面方向のベクトル

        myTransform.position = pos + deltapos;
        myTransform.eulerAngles = direction;

        if (actFrameCounter == framesPerOneAct)
        {
            actProcRunning = false;
            actFrameCounter = 0;
        }
    }

    public int Max(params int[] nums)
    {
        // 引数が渡されない場合
        if (nums.Length == 0) return 0;

        int max = nums[0];
        for (int i = 1; i < nums.Length; i++)
        {
            max = max > nums[i] ? max : nums[i];
            // Minの場合は不等号を逆にすればOK
        }
        return max;
    }

    public int Min(params int[] nums)
    {
        // 引数が渡されない場合
        if (nums.Length == 0) return 0;

        int min = nums[0];
        for (int i = 1; i < nums.Length; i++)
        {
            min = min < nums[i] ? min : nums[i];
        }
        return min;
    }
}
