using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class QLearning
{
    double alpha = 0.2f;
    double gamma = 0.99f;

    private ActSelUtility actionSelector;
    private int behaviorPolicy;
    private int estimationPolicy;
    private int policy;
    private bool polCh = false;

    List<List<double>> Q;

    // 学習率，割引率，各状態における行動可能な行動数(sizeが状態数となる), Q値の初期値，挙動方策, 推定方策
    public QLearning(double LearningRate, double DiscountRate, int[] sLenAndABind, double initQ, int BehaviorPolicy, int EstimationPolicy)
    {
        alpha = LearningRate;
        gamma = DiscountRate;
        
        Q = new List<List<double>>();
        for (int i = 0; i < sLenAndABind.Length; i++)
        {
            List<double> array = new List<double>(new double[sLenAndABind[i]]);
            Q.Add(array);
        }
        
        for (int i = 0; i < sLenAndABind.Length; i++)
        {
            for (int j = 0; j < sLenAndABind[i]; j++)
            {
                Q[i][j] = initQ;
            }
        }

        actionSelector = new ActSelUtility();
        behaviorPolicy = BehaviorPolicy;
        estimationPolicy = EstimationPolicy;
        policy = behaviorPolicy;
    }
     public void ValueUpdate(int s, int a, int ss, double r)
    {
        Q[s][a] = Q[s][a] + alpha * (r + gamma * Q[ss][actionSelector.greedy_selection(Q[ss])] - Q[s][a]);
    }

    public int SelectAction(int s)
    {
        int tardex = 0;

        switch (policy)
        {
            case ActSelUtility.GREEDY:
                tardex = actionSelector.greedy_selection(Q[s]);
                break;
            case ActSelUtility.RANDOM:
                tardex = actionSelector.random_selection(Q[s]);
                break;
            case ActSelUtility.EPSGREEDY:
                tardex = actionSelector.epsilon_greedy_selection(Q[s]);
                break;
            case ActSelUtility.SOFTMAX:
                tardex = actionSelector.softmax_selection(Q[s]);
                break;
        }

        return tardex;
    }

    public void switchPolicy()
    {
        if (polCh) {
            policy = behaviorPolicy;
            Debug.Log("switched behave policy");
        }
        else {
            policy = estimationPolicy;
            Debug.Log("switched estimate policy");
        }
        polCh = !polCh;
    }

    public void fixActionSelector(double Epsilon, double Tau)
    {
        actionSelector = new ActSelUtility(Epsilon, Tau);
    }

    public void loadQ(string path)
    {
        string data = File.ReadAllText(path);
        string[] data2 = data.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

        for (int i = 0; i < data2.Length; i++)
        {
            for (int j = 0; j < data2[i].Split(',').Length; j++)
            {
                Q[i][j] = Convert.ToDouble(data2[i].Split(',')[j]);
            }
        }
    }

    //Debug用関数
    public string QvaluesView()
    {
        string senten = "";

        for (int i = 0; i < Q.Count; i++)
        {
            for (int j = 0; j < Q[i].Count - 1; j++)
            {
                senten += Q[i][j].ToString() + ",";
            }
            senten += Q[i][Q[i].Count - 1].ToString() + "\n";
        }
        return senten;
    }
}
