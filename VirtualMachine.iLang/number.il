Max : [a:Number, b:Number] => Number {
	if (a > b) then {
		return a;
	} else {
		return b;
	}
}; 

Min : [a:Number, b:Number] => Number {
	if (a < b) then {
		return a;
	} else {
		return b;
	}
};

Abs : [a:Number] => Number {
	if (a < 0) then {
		return -a;
	} else {
		return a;
	}
};	

Sum : [a:Number, b:Number] => Number {
	if (a = b) then {
		return 0;
	} else {
		return a + Sum(a + 1, b);
	}
};

Product : [a:Number, b:Number] => Number {
	if (a = b) then {
		return 1;
	} else {
		return a * Product(a + 1, b);
	}
};

Factorial : [a:Number] => Number {
	if (a = 0) then {
		return 1;
	} else {
		return a * Factorial(a - 1);
	}
};

Fibonacci : [a:Number] => Number {
	if (a < 2) then {
		return a;
	} else {
		return Fibonacci(a - 1) + Fibonacci(a - 2);
	}
};

IsPrime : [n:Number] => Logic {
    if (n < 3) then {
        return false;
    } else {
        let i : Number = 2;
        if ((n % 2) = 0) then {
			return n = 2;
		} else {
            i <- 3;
            while ((i * i) < n) do {
                if((n % i) = 0) then {
                    return false;
                } else {
                    i <- i + 2;
                }
            }
            return true;
        }
    }
};
