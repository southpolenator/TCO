import java.io.*;
import java.security.*;
import java.util.*;

public class InvestmentAdviceVis {

Random r;
int numExperts;
double[] accuracy;
double[] stDev;
int numPeriods;

double[] actual;
int[] reported;
int[] result;
int money = 1000000;

int timeLeft = 10000;

private void writeOutput(String s) {
  if (!vis) return;
  System.out.println(s);
}

public String checkData(String s) { return ""; }

public String displayTestCase(String s) {
  generateTestCase(Long.parseLong(s));
  StringBuilder sb = new StringBuilder();
  sb.append("Num Periods = ").append(numPeriods).append("\n");
  for (int i = 0; i < numExperts; i++) {
    sb.append("Expert ").append(i).append(": accuracy= ").append(accuracy[i]).append(", stDev= ").append(stDev[i]).append("\n");
  }
  return sb.toString();
}

private void generateTestCase(long x) {
  r = new Random(x);
  numExperts = 10 + r.nextInt(41);
  stDev = new double[numExperts];
  accuracy = new double[numExperts];
  result = new int[numExperts];
  actual = new double[numExperts];
  reported = new int[numExperts];
  for (int i = 0; i < numExperts; i++) {
    stDev[i] = r.nextDouble() * 20;
	accuracy[i] = r.nextDouble();
  }
  numPeriods = 10 + r.nextInt(91);
}

private void generateNextDay() {
  for (int i = 0; i < numExperts; i++) {
    actual[i] = Math.min(1, Math.max(-1, r.nextGaussian() * 0.1));
	if (r.nextDouble() < accuracy[i]) {
      reported[i] = (int)Math.round(Math.min(100, Math.max(-100, r.nextGaussian() * stDev[i] + actual[i] * 100)));
	} else {
      reported[i] = (int)Math.round(100 * Math.min(1, Math.max(-1, r.nextGaussian() * 0.1)));
	}
  }
}

private void applyResults(int[] invest) {
  for (int i = 0; i < invest.length; i++) {
    result[i] = (int)Math.floor(invest[i] * actual[i]);
    money += result[i];
  }
}

private String validateOutput(int[] invest) {
  if (invest.length != numExperts) return "Return was the wrong length.";
  int total = 0;
  for (int i = 0; i < invest.length; i++) {
    total += invest[i];
    if (invest[i] < 0) return "Investing a negative amount is not allowed.";
    if (invest[i] > 400000) return "Investing more than 400,000 with a single expert is not allowed.";
  }
  if (total > money) return "Attempted to invest more money than currently held.";
  return "";
}

// ------------- visualization part ------------
static String exec;
static boolean vis, debug;
static Process proc;
InputStream is;
OutputStream os;
BufferedReader br;

private int[] getInvestments(int[] advice, int[] recent, int money, int timeLeft, int roundsLeft) throws IOException {
  StringBuffer sb = new StringBuffer();
  sb.append(advice.length).append("\n");
  for (int i = 0; i < advice.length; i++) sb.append(advice[i]).append("\n");
  sb.append(recent.length).append("\n");
  for (int i = 0; i < recent.length; i++) sb.append(recent[i]).append("\n");
  sb.append(money).append("\n");
  sb.append(timeLeft).append("\n");
  sb.append(roundsLeft).append("\n");
  os.write(sb.toString().getBytes());
  os.flush();
  int[] ret = new int[Integer.parseInt(br.readLine())];
  for (int i = 0; i < ret.length; i++) ret[i] = Integer.parseInt(br.readLine());
  return ret;
}

public double runTest(String testValue) {
  long seed = Long.parseLong(testValue);
  generateTestCase(seed);
  for (int i = 0; i < numPeriods; i++) {
    generateNextDay();
    long start = System.currentTimeMillis();
    int[] invest;
    try {
      invest = getInvestments(reported, result, money, timeLeft, numPeriods - i);
    } catch (Exception e) {
      writeOutput("Error calling getInvestments()");
      return -1;
    }
    long end = System.currentTimeMillis();
    long elapsed = end - start;
    timeLeft -= elapsed;
    if (timeLeft < 10) {
      writeOutput("Time limit exceeded.");
      return -1;
    }
    String error = validateOutput(invest);
    if (error != "") {
      writeOutput(error);
      return -1;
    }
    applyResults(invest);
  }
  return money;
}

    public InvestmentAdviceVis(String seed) {
      try {
        if (exec != null) {
            try {
                Runtime rt = Runtime.getRuntime();
                proc = rt.exec(exec);
                os = proc.getOutputStream();
                is = proc.getInputStream();
                br = new BufferedReader(new InputStreamReader(is));
                new ErrorReader(proc.getErrorStream()).start();
            } catch (Exception e) { e.printStackTrace(); }
        }
        System.out.println("Score = " + runTest(seed));
        if (proc != null)
            try { proc.destroy(); } 
            catch (Exception e) { e.printStackTrace(); }
      }
      catch (Exception e) { e.printStackTrace(); }
    }
    // -----------------------------------------
    public static void main(String[] args) {
        String seed = "1";
        vis = true;
        for (int i = 0; i<args.length; i++)
        {   if (args[i].equals("-seed"))
                seed = args[++i];
            if (args[i].equals("-exec"))
                exec = args[++i];
            if (args[i].equals("-novis"))
                vis = false;
            if (args[i].equals("-debug"))
                debug = true;
        }

        InvestmentAdviceVis f = new InvestmentAdviceVis(seed);
    }
}

class ErrorReader extends Thread{
    InputStream error;
    public ErrorReader(InputStream is) {
        error = is;
    }
    public void run() {
        try {
            byte[] ch = new byte[50000];
            int read;
            while ((read = error.read(ch)) > 0)
            {   String s = new String(ch,0,read);
                System.out.print(s);
                System.out.flush();
            }
        } catch(Exception e) { }
    }
}
